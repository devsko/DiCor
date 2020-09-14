using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Bedrock.Framework;
using Bedrock.Framework.Protocols;

using Microsoft.AspNetCore.Connections;
using Microsoft.VisualStudio.Threading;

namespace DiCor.Net.UpperLayer
{
    public partial class ULConnection
    {
        private const int AbortTimeout = 500;
        private const int RequestTimeout = 500;

        public static async Task<ULConnection> AssociateAsync(Client client, EndPoint endpoint, AssociationType type, CancellationToken cancellationToken = default)
        {
            if (client is null)
                throw new ArgumentNullException(nameof(client));
            if (endpoint is null)
                throw new ArgumentNullException(nameof(endpoint));

            ULConnection ulConnection = new(type);
            await ulConnection.AssociateAsync(client, endpoint, cancellationToken).ConfigureAwait(false);

            return ulConnection;
        }

        public static async Task<ULConnection> StartServiceAsync(ConnectionContext connection, CancellationToken cancellationToken = default)
        {
            if (connection is null)
                throw new ArgumentNullException(nameof(connection));

            ULConnection ulConnection = new(connection);
            await ulConnection.StartServiceAsync(cancellationToken).ConfigureAwait(false);

            return ulConnection;
        }

        private readonly Protocol _protocol;
        private readonly AsyncSemaphore _semaphore = new(initialCount: 1);
        private readonly CancellationTokenSource _cts = new();

        private ConnectionContext? _connection;
        private Association? _association;
        private ULConnectionState _state;
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

        public ULConnectionState State => _state;

        public Association? Association => _association;

        private async Task AssociateAsync(Client client, EndPoint endpoint, CancellationToken cancellationToken = default)
        {
            using (await _semaphore.EnterAsync(cancellationToken).ConfigureAwait(false))
            {
                if (_state != ULConnectionState.Sta1_Idle)
                    throw new ULProtocolException(ULConnectionState.Sta1_Idle, _state);

                await AE1_IssueTransportConnect(client, endpoint, cancellationToken).ConfigureAwait(false);
                _ = ReadLoopAsync();
                await AE2_SendAAssociateRq(cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task StartServiceAsync(CancellationToken cancellationToken)
        {
            using (await _semaphore.EnterAsync(cancellationToken).ConfigureAwait(false))
            {
                if (_state != ULConnectionState.Sta1_Idle)
                    throw new ULProtocolException(ULConnectionState.Sta1_Idle, _state);

                _ = ReadLoopAsync();
                await AE5_IssueTransportResponse(cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task ReleaseAsync()
        {

        }

        public async Task AbortAsync()
        {

        }

#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods

        private async Task AE1_IssueTransportConnect(Client client, EndPoint endpoint, CancellationToken cancellationToken)
        {
            Debug.Assert(_state == ULConnectionState.Sta1_Idle);

            _connection = await client.ConnectAsync(endpoint, cancellationToken).ConfigureAwait(false);
            await SetStateAsync(ULConnectionState.Sta4_AwaitingTransportConnectionOpen, null);
        }

        private async Task AE2_SendAAssociateRq(CancellationToken cancellationToken)
        {
            Debug.Assert(_state == ULConnectionState.Sta4_AwaitingTransportConnectionOpen);

            await _writer!.WriteAsync(_protocol, new ULMessage(Pdu.Type.AAssociateRq), cancellationToken).ConfigureAwait(false);
            await SetStateAsync(ULConnectionState.Sta5_AwaitingAssociateResponse,
                new ResponseAwaiter(cancellationToken)).ConfigureAwait(false);
        }

        private async Task AE3_IssueAAssociateAccept()
        {
            Debug.Assert(_state == ULConnectionState.Sta5_AwaitingAssociateResponse);

            await SetStateAsync(ULConnectionState.Sta6_Ready, null).ConfigureAwait(false);
        }

        private async Task AE4_IssueAAssociateReject()
        {
            Debug.Assert(_state == ULConnectionState.Sta5_AwaitingAssociateResponse);
            Debug.Assert(_connection != null);

            await _connection.DisposeAsync().ConfigureAwait(false);
            _connection = null;
            await SetStateAsync(ULConnectionState.Sta1_Idle, null).ConfigureAwait(false);
        }

        private async Task AE5_IssueTransportResponse(CancellationToken cancellationToken)
        {
            await SetStateAsync(ULConnectionState.Sta2_TransportConnectionOpen,
                new ResponseAwaiter(cancellationToken, RequestTimeout)).ConfigureAwait(false);
        }

        private async Task AA1_SendAAbort(CancellationToken cancellationToken)
        {
            await _writer!.WriteAsync(_protocol, new ULMessage(Pdu.Type.AAbort, (byte)Pdu.AbortSource.ServiceUser)).ConfigureAwait(false);
            await SetStateAsync(ULConnectionState.Sta13_AwaitingTransportConnectionClose,
                new ResponseAwaiter(cancellationToken, timeout: AbortTimeout)).ConfigureAwait(false);
        }

#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods

        private Task SetStateAsync(ULConnectionState state, ResponseAwaiter? responseAwaiter)
        {
            ResponseAwaiter? oldAwaiter = _responseAwaiter;
            _state = state;
            _responseAwaiter = responseAwaiter;
            oldAwaiter?.TrySetResult();

            return responseAwaiter?.Task ?? Task.CompletedTask;
        }

        private async Task ReadLoopAsync()
        {
            _reader = _connection.CreateReader();
            _writer = _connection.CreateWriter();

            while (true)
            {
                ProtocolReadResult<ULMessage> result = await _reader.ReadAsync(_protocol, (int)Association.DefaultMaxDataLength, _cts.Token);
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

            using (await _semaphore.EnterAsync().ConfigureAwait(false))
            {
                switch (_state)
                {
                    case ULConnectionState.Sta1_Idle:
                    case ULConnectionState.Sta4_AwaitingTransportConnectionOpen:
                        // Cannot receive anything without connection.
                        throw new ULProtocolException("How did we receive a message without connection?");

                    case ULConnectionState.Sta2_TransportConnectionOpen:
                        await AA1_SendAAbort(default).ConfigureAwait(false);
                        break;

                    case ULConnectionState.Sta5_AwaitingAssociateResponse:
                        await (message.Type switch
                        {
                            Pdu.Type.AAssociateAc => AE3_IssueAAssociateAccept(),
                            Pdu.Type.AAssociateRj => AE4_IssueAAssociateReject(),
                            _ => Task.CompletedTask,
                        }).ConfigureAwait(false);
                        break;

                    case ULConnectionState.Sta13_AwaitingTransportConnectionClose:
                        // AA-6 IGNORE
                        break;

                    default:
                        // AA-8
                        await _writer.WriteAsync(_protocol, new ULMessage(Pdu.Type.AAbort, (byte)Pdu.AbortSource.ServiceProvider)).ConfigureAwait(false);
                        // TODO Start ARTIM
                        _state = ULConnectionState.Sta13_AwaitingTransportConnectionClose;
                        break;
                }
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
