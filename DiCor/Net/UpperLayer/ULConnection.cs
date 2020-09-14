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
        private const int AbortTimeout = 500;
        private const int RequestTimeout = 500;

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

        public static async Task<ULConnection> StartServiceAsync(ConnectionContext connection, CancellationToken cancellationToken = default)
        {
            if (connection is null)
                throw new ArgumentNullException(nameof(connection));

            var ulConnection = new ULConnection(connection);
            await ulConnection.StartServiceAsync(cancellationToken).ConfigureAwait(false);

            return ulConnection;
        }

        private readonly Protocol _protocol;
        private readonly object _lock = new();
        private readonly CancellationTokenSource _readLoopCts = new();

        private ConnectionContext? _connection;
        private Association? _association;
        private State _state;
        private ProtocolReader? _reader;
        private ProtocolWriter? _writer;
        private ResponseAwaiter? _responseAwaiter;

        private ULConnection(AssociationType type)
        {
            _association = new Association(type);
            _protocol = new(this);
        }

        private ULConnection(ConnectionContext connection)
        {
            _connection = connection;
            _protocol = new(this);
        }

        public State CurrentState => _state;

        public Association? Association => _association;

        private async Task AssociateAsync(Client client, EndPoint endpoint, CancellationToken cancellationToken = default)
        {
            if (_state != State.Sta1_Idle)
                throw new ULProtocolException(State.Sta1_Idle, _state);

            await AE1_IssueTransportConnect(client, endpoint, cancellationToken).ConfigureAwait(false);
            _ = ReadLoopAsync();
            await AE2_SendAAssociateRq(cancellationToken).ConfigureAwait(false);
        }

        private async Task StartServiceAsync(CancellationToken cancellationToken)
        {
            if (_state != State.Sta1_Idle)
                throw new ULProtocolException(State.Sta1_Idle, _state);

            _ = ReadLoopAsync();
            await AE5_IssueTransportResponse(cancellationToken).ConfigureAwait(false);
        }

        //public async Task ReleaseAsync()
        //{

        //}

        //public async Task AbortAsync()
        //{

        //}

        public ValueTask DisposeAsync()
        {
            _readLoopCts.Cancel();
            ConnectionContext? connection = _connection;
            _connection = null;
            if (connection != null)
                return connection.DisposeAsync();

            return ValueTask.CompletedTask;
        }

#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods

        private async Task AE1_IssueTransportConnect(Client client, EndPoint endpoint, CancellationToken cancellationToken)
        {
            ValueTask<ConnectionContext> connect;
            lock (_lock)
            {
                if (_state != State.Sta1_Idle)
                    throw new ULProtocolException(State.Sta1_Idle, _state);

                connect = client.ConnectAsync(endpoint, cancellationToken);
                SetState(State.Sta4_AwaitingTransportConnectionOpen);
            }
            _connection = await connect.ConfigureAwait(false);
        }

        private async Task AE2_SendAAssociateRq(CancellationToken cancellationToken)
        {
            ValueTask write;
            lock (_lock)
            {
                Debug.Assert(_state == State.Sta4_AwaitingTransportConnectionOpen);

                write = _writer!.WriteAsync(_protocol, new ULMessage(Pdu.Type.AAssociateRq, @object: Association), cancellationToken);
                SetState(State.Sta5_AwaitingAssociateResponse);
            }
            await write.ConfigureAwait(false);
            await new ResponseAwaiter(this, cancellationToken).Task.ConfigureAwait(false);
        }

        private Task AE3_IssueAAssociateAccepted(ULMessage message)
        {
            lock (_lock)
            {
                Debug.Assert(_state == State.Sta5_AwaitingAssociateResponse);

                _association = (Association)message.Object!;
                // TODO Event ?
                SetState(State.Sta6_Ready);
            }
            return Task.CompletedTask;
        }

        private async Task AE4_IssueAAssociateRejected()
        {
            ValueTask dispose;
            lock (_lock)
            {
                Debug.Assert(_state == State.Sta5_AwaitingAssociateResponse);
                Debug.Assert(_connection != null);

                // TODO Event ?
                dispose = DisposeAsync();
                SetState(State.Sta1_Idle);
            }
            await dispose.ConfigureAwait(false);
        }

        private async Task AE5_IssueTransportResponse(CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                SetState(State.Sta2_TransportConnectionOpen);
            }
            try
            {
                await new ResponseAwaiter(this, cancellationToken, RequestTimeout).Task.ConfigureAwait(false);
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken == _responseAwaiter!.TimeoutToken)
            {
                lock (_lock)
                {
                    SetState(State.Sta1_Idle);
                }
            }
        }

        private async Task AA1_SendAAbort(CancellationToken cancellationToken)
        {
            ValueTask write;
            lock (_lock)
            {
                write = _writer!.WriteAsync(_protocol, new ULMessage(Pdu.Type.AAbort, (byte)Pdu.AbortSource.ServiceUser));
                SetState(State.Sta13_AwaitingTransportConnectionClose);
            }
            await write.ConfigureAwait(false);
            await new ResponseAwaiter(this, cancellationToken, timeout: AbortTimeout).Task.ConfigureAwait(false);
        }

#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods

        private void SetState(State state)
        {
            ResponseAwaiter? oldAwaiter = _responseAwaiter;
            _state = state;
            _responseAwaiter = null;
            oldAwaiter?.TrySetResult();
        }

        private async Task ReadLoopAsync()
        {
            _reader = _connection.CreateReader();
            _writer = _connection.CreateWriter();

            while (true)
            {
                ProtocolReadResult<ULMessage> result = await _reader.ReadAsync(_protocol, (int)Association.DefaultMaxDataLength, _readLoopCts.Token);
                if (result.IsCompleted)
                    break;
                _reader.Advance();

                await ProcessAsync(result.Message).ConfigureAwait(false);
            }
        }

        private ValueTask ProcessAsync(ULMessage message)
            => message.Type switch
            {
                Pdu.Type.AAssociateAc or
                Pdu.Type.AAssociateRj
                    => OnAAssociateResponseAsync(message),
                Pdu.Type.AAbort
                    => OnAAbortAsync(message),

                _ => throw new ArgumentException(nameof(message)),
            };

        private async ValueTask OnAAssociateResponseAsync(ULMessage message)
        {
            Debug.Assert(_connection != null);
            Debug.Assert(_writer != null);

            switch (_state)
            {
                case State.Sta1_Idle:
                case State.Sta4_AwaitingTransportConnectionOpen:
                    // Cannot receive anything without connection.
                    throw new ULProtocolException("How did we receive a message without connection?");

                case State.Sta2_TransportConnectionOpen:
                    await AA1_SendAAbort(default).ConfigureAwait(false);
                    break;

                case State.Sta5_AwaitingAssociateResponse:
                    await (message.Type switch
                    {
                        Pdu.Type.AAssociateAc => AE3_IssueAAssociateAccepted(message),
                        Pdu.Type.AAssociateRj => AE4_IssueAAssociateRejected(),
                        _ => Task.CompletedTask,
                    }).ConfigureAwait(false);
                    break;

                case State.Sta13_AwaitingTransportConnectionClose:
                    // AA-6 IGNORE
                    break;

                default:
                    // AA-8
                    await _writer.WriteAsync(_protocol, new ULMessage(Pdu.Type.AAbort, (byte)Pdu.AbortSource.ServiceProvider)).ConfigureAwait(false);
                    // TODO Start ARTIM
                    _state = State.Sta13_AwaitingTransportConnectionClose;
                    break;
            }
        }
        private async ValueTask OnAAbortAsync(ULMessage message)
        {
            ConnectionContext? connection = _connection;
            if (connection != null)
                await connection.DisposeAsync().ConfigureAwait(false);
        }

    }
}
