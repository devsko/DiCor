using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Bedrock.Framework;
using Bedrock.Framework.Protocols;

using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DiCor.Net.UpperLayer
{
    public partial class ULConnection : IAsyncDisposable
    {
        private const int RequestTimeout = 500;
        private const int RejectTimeout = 500;
        private const int AbortTimeout = 500;

        public event EventHandler<AAssociateArgs>? AAssociate;
        public event EventHandler<AAbortArgs>? AAbort;
        public event EventHandler<APAbortArgs>? APAbort;

        private readonly ILoggerFactory _loggerFactory;
        private readonly Protocol _protocol;
        private readonly object _lock = new();

        private ILogger _logger;
        private ConnectionContext? _connection;
        private Association? _association;
        private bool? _isServiceProvider;
        private State _state;
        private ProtocolReader? _reader;
        private ProtocolWriter? _writer;
        private Task? _readLoopTask;
        private CancellationTokenSource? _readLoopCts;
        private ResponseAwaiter? _responseAwaiter;

        public ULConnection(ILoggerFactory? loggerFactory = null)
        {
            _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
            _logger = NullLogger.Instance;
            _protocol = new(this);
        }

        public State CurrentState => _state;

        public Association? Association => _association;

        public async Task<Task> StartServiceAsync(ConnectionContext connection, CancellationToken cancellationToken = default)
        {
            if (_isServiceProvider != null)
                throw new InvalidOperationException();

            _isServiceProvider = true;
            _logger = _loggerFactory.CreateLogger("ULConnection (Server)");
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));

            _logger.LogDebug($"Creating server connection {connection.ConnectionId}");

            _readLoopTask = ReadLoopAsync();
            await AE5_AcceptConnection(cancellationToken).ConfigureAwait(false);

            return _readLoopTask;
        }

        public ValueTask AcceptAssociationAsync(ULMessage message, CancellationToken cancellationToken)
        {
            return AE7_SendAAssociateAc(message, cancellationToken);
        }

        public Task RejectAssociationAsync(ULMessage message, CancellationToken cancellationToken)
        {
            return AE8_SendAAssociateRj(message, cancellationToken);
        }

        //public async Task ReleaseAsync()
        //{

        //}

        public async Task AssociateAsync(Client client, EndPoint endpoint, AssociationType type, CancellationToken cancellationToken = default)
        {
            if (client is null)
                throw new ArgumentNullException(nameof(client));
            if (endpoint is null)
                throw new ArgumentNullException(nameof(endpoint));
            if (!Enum.IsDefined<AssociationType>(type))
                throw new ArgumentException(null, nameof(type));
            if (_isServiceProvider != null)
                throw new InvalidOperationException();

            _isServiceProvider = false;
            _logger = _loggerFactory.CreateLogger("ULConnection (Client)");
            _association = new Association(type);

            _logger.LogDebug($"Creating client connection to {endpoint}");

            await AE1_Connect(client, endpoint, cancellationToken).ConfigureAwait(false);
            _readLoopTask = ReadLoopAsync();
            await AE2_SendAAssociateRq(cancellationToken).ConfigureAwait(false);

            if (!await ResponseAwaiter.AwaitResponseAsync(this, cancellationToken).ConfigureAwait(false))
            {
                await DisposeAsync().ConfigureAwait(false);
            }
        }

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

            _logger.LogTrace("Disposing");

            ConnectionContext connection = _connection;
            _connection = null;
            _state = State.Sta1_Idle;

            await _writer!.DisposeAsync().ConfigureAwait(false);
            connection.Transport.Input.CancelPendingRead();
            _readLoopCts!.Cancel();
            await _readLoopTask!.ConfigureAwait(false);
            await _reader!.DisposeAsync().ConfigureAwait(false);
            await connection.DisposeAsync().ConfigureAwait(false);

            _logger.LogTrace("Disposed");
        }

        private void SetState(State state)
        {
            _logger.LogTrace($"Change state to {state}");

            ResponseAwaiter? oldAwaiter = _responseAwaiter;
            _state = state;
            _responseAwaiter = null;
            oldAwaiter?.Completed();
        }

        private async Task ReadLoopAsync()
        {
            Debug.Assert(_connection != null);

            _logger.LogTrace("Starting read loop");

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
                            => OnAAssociateRqAsync(message, cancellationToken),
                        Pdu.Type.AAssociateAc
                            => OnAAssociateAcAsync(message, cancellationToken),
                        Pdu.Type.AAssociateRj
                            => OnAAssociateRjAsync(message, cancellationToken),
                        Pdu.Type.AAbort
                            => OnAAbortAsync(message, cancellationToken).AsTask(),

                        _ => OnUnrecognizedPduAsync(message, cancellationToken),
                    }).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
            { }

            _logger.LogTrace("Read loop finished");

            OnConnectionClosed();
        }

        private void OnConnectionClosed()
        {
            _responseAwaiter?.Unblock();

            if (_connection == null)
                return;

            _logger.LogDebug($"Connection closed {_connection!.ConnectionId}");

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

        private Task OnAAssociateRqAsync(ULMessage message, CancellationToken cancellationToken)
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

        private Task OnAAssociateAcAsync(ULMessage message, CancellationToken cancellationToken)
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

        private Task OnAAssociateRjAsync(ULMessage message, CancellationToken cancellationToken)
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

        private ValueTask OnAAbortAsync(ULMessage message, CancellationToken cancellationToken)
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
