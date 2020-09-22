using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Bedrock.Framework;
using Bedrock.Framework.Protocols;

using Microsoft.AspNetCore.Connections;

namespace DiCor.Net.UpperLayer
{
    public partial class ULConnection : IAsyncDisposable
    {
        public static async Task<ULConnection> AssociateAsync(Client client, EndPoint endpoint, AssociationType type, CancellationToken cancellationToken = default)
        {
            if (client is null)
                throw new ArgumentNullException(nameof(client));
            if (endpoint is null)
                throw new ArgumentNullException(nameof(endpoint));

            var ulConnection = new ULConnection(type);
            await ulConnection.AssociateAsync(client, endpoint, cancellationToken).ConfigureAwait(false);

            return ulConnection;
        }

        public static Task<Task> StartServiceAsync(ConnectionContext connection, CancellationToken cancellationToken = default)
        {
            if (connection is null)
                throw new ArgumentNullException(nameof(connection));

            var ulConnection = new ULConnection(connection);

            return ulConnection.StartServiceAsync(cancellationToken);
        }

        private const int RequestTimeout = 500;
        private const int RejectTimeout = 500;
        private const int AbortTimeout = 500;

        public event EventHandler<AAssociateArgs>? AAssociate;
        public event EventHandler<AAbortArgs>? AAbort;
        public event EventHandler<APAbortArgs>? APAbort;

        private readonly Protocol _protocol;
        private readonly object _lock = new();

        private ConnectionContext? _connection;
        private Association? _association;
        private bool _isServiceProvider;
        private State _state;
        private ProtocolReader? _reader;
        private ProtocolWriter? _writer;
        private Task? _readLoopTask;
        private CancellationTokenSource? _readLoopCts;
        private ResponseAwaiter? _responseAwaiter;

        private ULConnection(AssociationType type)
        {
            _association = new Association(type);
            _protocol = new(this);
        }

        private ULConnection(ConnectionContext connection)
        {
            _isServiceProvider = true;
            _connection = connection;
            _protocol = new(this);
        }

        public State CurrentState => _state;

        public Association? Association => _association;

        private async Task AssociateAsync(Client client, EndPoint endpoint, CancellationToken cancellationToken = default)
        {
            await AE1_Connect(client, endpoint, cancellationToken).ConfigureAwait(false);
            _readLoopTask = ReadLoopAsync();
            await AE2_SendAAssociateRq(cancellationToken).ConfigureAwait(false);
        }

        private async Task<Task> StartServiceAsync(CancellationToken cancellationToken)
        {
            _readLoopTask = ReadLoopAsync();
            await AE5_AcceptConnection(cancellationToken).ConfigureAwait(false);

            return _readLoopTask;
        }

        public ValueTask AcceptAssociationAsync(ULMessage<AAssociateAcData> message, CancellationToken cancellationToken)
        {
            return AE7_SendAAssociateAc(message, cancellationToken);
        }

        public Task RejectAssociationAsync(ULMessage<AAssociateRjData> message, CancellationToken cancellationToken)
        {
            return AE8_SendAAssociateRj(message, cancellationToken);
        }

        //public async Task ReleaseAsync()
        //{

        //}

        public Task AbortAsync(CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                return _state == State.Sta4_AwaitingTransportConnectionOpen
                    ? AA2_CloseConnection().AsTask()
                    : AA1_SendAAbort(cancellationToken);
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_connection == null)
                return;

            ConnectionContext connection = _connection;
            _connection = null;

            await _writer!.DisposeAsync().ConfigureAwait(false);
            connection.Transport.Input.CancelPendingRead();
            _readLoopCts!.Cancel();
            await _readLoopTask!.ConfigureAwait(false);
            await _reader!.DisposeAsync().ConfigureAwait(false);
            await connection.DisposeAsync().ConfigureAwait(false);
        }

        private void SetState(State state)
        {
            ResponseAwaiter? oldAwaiter = _responseAwaiter;
            _state = state;
            _responseAwaiter = null;
            oldAwaiter?.TrySetResult();
        }

        private async Task ReadLoopAsync()
        {
            Debug.Assert(_connection != null);

            using (_connection.ConnectionClosed.Register(s => ((ULConnection)s!).OnConnectionClosed(), this))
            {
                _reader = _connection.CreateReader();
                _writer = _connection.CreateWriter();

                _readLoopCts = new CancellationTokenSource();
                CancellationToken cancellationToken = _readLoopCts.Token;
                try
                {
                    while (true)
                    {
                        ProtocolReadResult<ULMessage> result = await _reader.ReadAsync(_protocol, (int)Association.DefaultMaxDataLength);

                        if (result.IsCompleted || result.IsCanceled)
                            break;

                        _reader.Advance();

                        ULMessage message = result.Message;
                        await (message.Type switch
                        {
                            Pdu.Type.AAssociateRq
                                => OnAAssociateRqAsync(message.To<AAssociateRqData>(), cancellationToken),
                            Pdu.Type.AAssociateAc
                                => OnAAssociateAcAsync(message.To<AAssociateAcData>(), cancellationToken),
                            Pdu.Type.AAssociateRj
                                => OnAAssociateRjAsync(message.To<AAssociateRjData>(), cancellationToken),
                            Pdu.Type.AAbort
                                => OnAAbortAsync(message.To<AAbortData>(), cancellationToken).AsTask(),

                            _ => OnUnrecognizedPduAsync(message, cancellationToken),
                        }).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
                { }
            }
        }

        private void OnConnectionClosed()
        {
            lock (_lock)
            {
                _ = _state switch
                {
                    State.Sta2_TransportConnectionOpen
                        => AA5_StopTimer(),
                    State.Sta13_AwaitingTransportConnectionClose
                        => AR5_StopTimer(),

                    _ => AA4_IndicateAbort(),
                };
            }
        }

        private Task OnUnrecognizedPduAsync(ULMessage message, CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                return _state switch
                {
                    State.Sta2_TransportConnectionOpen
                        => AA1_SendAAbort(cancellationToken),
                    State.Sta13_AwaitingTransportConnectionClose
                        => AA7_SendAAbort(cancellationToken).AsTask(),

                    _ => AA8_SendAndIndicateAAbort(cancellationToken),
                };
            }
        }

        private Task OnAAssociateRqAsync(ULMessage<AAssociateRqData> message, CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                return _state switch
                {
                    State.Sta2_TransportConnectionOpen
                        => AE6_IndicateAssociate(message, cancellationToken),
                    State.Sta13_AwaitingTransportConnectionClose
                        => AA7_SendAAbort(cancellationToken).AsTask(),

                    _ => AA8_SendAndIndicateAAbort(cancellationToken),
                };
            }
        }

        private Task OnAAssociateAcAsync(ULMessage<AAssociateAcData> message, CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                return _state switch
                {
                    State.Sta2_TransportConnectionOpen
                        => AA1_SendAAbort(cancellationToken),
                    State.Sta5_AwaitingAssociateResponse
                        => AE3_ConfirmAAssociateAc(message),
                    State.Sta13_AwaitingTransportConnectionClose
                        => AA6_Ignore(),

                    _ => AA8_SendAndIndicateAAbort(cancellationToken),
                };
            }
        }

        private Task OnAAssociateRjAsync(ULMessage<AAssociateRjData> message, CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                return _state switch
                {
                    State.Sta2_TransportConnectionOpen
                        => AA1_SendAAbort(cancellationToken),
                    State.Sta5_AwaitingAssociateResponse
                        => AE4_ConfirmAAssociateRj(message).AsTask(),
                    State.Sta13_AwaitingTransportConnectionClose
                        => AA6_Ignore(),

                    _ => AA8_SendAndIndicateAAbort(cancellationToken),
                };
            }
        }

        private ValueTask OnAAbortAsync(ULMessage<AAbortData> message, CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                return _state switch
                {
                    State.Sta2_TransportConnectionOpen or
                    State.Sta13_AwaitingTransportConnectionClose
                        => AA2_CloseConnection(),

                    _ => AA3_IndicateAbortAndCloseConnection(),
                };
            }
        }

    }
}
