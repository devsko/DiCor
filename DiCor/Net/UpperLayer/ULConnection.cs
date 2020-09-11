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
    public enum ULConnectionState
    {
        Sta1_Idle,
        Sta2_TransportConnectionOpen,
        Sta3_AwaitingLocalAssociateResponse,
        Sta4_AwaitingTransportConnectionOpen,
        Sta5_AwaitingAssociateResponse,
        Sta6_Ready,
        Sta7_AwaitingReleaseResponse,
        Sta8_AwaitingLocalReleaseResponse,
        Sta9_AwaitingLocalReleaseResponseCollisionRequestor,
        Sta10_AwaitingReleaseResponseCollisionAcceptor,
        Sta11_AwaitingReleaseResponseCollisionRequestor,
        Sta12_AwaitingLocalReleaseResponseCollisionAcceptor,
        Sta13_AwaitingTransportConnectionClose,

    }

    public partial class ULConnection
    {
        private const int SendAAbortTimeout = 500;

        public static async Task<ULConnection> AssociateAsync(Client client, EndPoint endpoint, AssociationType type, CancellationToken cancellationToken = default)
        {
            ULConnection connection = new(client, endpoint, type);
            await connection.AssociateAsync(cancellationToken).ConfigureAwait(false);

            return connection;
        }

        private readonly Client _client;
        private readonly EndPoint _endpoint;
        private readonly Association _association;
        private readonly Protocol _protocol;
        private readonly object _stateLock = new();

        private ConnectionContext? _connection;
        private ULConnectionState _state;
        private ProtocolReader? _reader;
        private ProtocolWriter? _writer;
        private ResponseAwaiter? _responseAwaiter;

        private ULConnection(Client client, EndPoint endpoint, AssociationType type)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            _association = new Association(type);
            _protocol = new Protocol(this);
        }

        public ULConnectionState State => _state;

        public Association Association => _association;

        private async Task AssociateAsync(CancellationToken cancellationToken = default)
        {
            if (_state != ULConnectionState.Sta1_Idle)
                // TODO ProtocolException
                throw new InvalidOperationException();

            _state = ULConnectionState.Sta4_AwaitingTransportConnectionOpen;

            // AE-1: Issue TRANSPORT CONNECT request primitive to local transport service
            _connection = await _client.ConnectAsync(_endpoint, cancellationToken).ConfigureAwait(false);

            _reader = _connection.CreateReader();
            _writer = _connection.CreateWriter();

            _ = ReadLoopAsync(cancellationToken);

            await AE2_SendAAssociateRq(cancellationToken).ConfigureAwait(false);
        }

#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
        private async Task AE2_SendAAssociateRq(CancellationToken cancellationToken)
        {
            await _writer!.WriteAsync(_protocol, new ULMessage(Pdu.Type.AAssociateRq), cancellationToken).ConfigureAwait(false);
            await SetStateAsync(
                ULConnectionState.Sta5_AwaitingAssociateResponse,
                new ResponseAwaiter(cancellationToken, /* !!!! */ 100000)).ConfigureAwait(false);
        }

        private Task AE3_IssueAAssociateConfirmationPrimitive()
        {
            return SetStateAsync(ULConnectionState.Sta6_Ready, null);
        }

        private async Task AA1_SendAAbort(CancellationToken cancellationToken)
        {
            await _writer!.WriteAsync(_protocol, new ULMessage(Pdu.Type.AAbort, (byte)Pdu.AbortSource.ServiceUser)).ConfigureAwait(false);
            await SetStateAsync(ULConnectionState.Sta13_AwaitingTransportConnectionClose,
                new ResponseAwaiter(cancellationToken, timeout: SendAAbortTimeout)).ConfigureAwait(false);
        }
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods

        private Task SetStateAsync(ULConnectionState state, ResponseAwaiter? responseAwaiter)
        {
            lock (_stateLock)
            {
                ResponseAwaiter? oldAwaiter = _responseAwaiter;
                _state = state;
                _responseAwaiter = responseAwaiter;
                oldAwaiter?.TrySetResult();

                return responseAwaiter?.Task ?? Task.CompletedTask;
            }
        }

        private async Task ReadLoopAsync(CancellationToken cancellationToken)
        {
            Debug.Assert(_reader != null);

            while (true)
            {
                ProtocolReadResult<ULMessage> result = await _reader.ReadAsync(_protocol, (int)Association.DefaultMaxDataLength, cancellationToken);
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
                case ULConnectionState.Sta1_Idle:
                case ULConnectionState.Sta4_AwaitingTransportConnectionOpen:
                    // Cannot receive anything without connection.
                    throw new InvalidOperationException("How did we receive a message without connection?");

                case ULConnectionState.Sta2_TransportConnectionOpen:
                    await AA1_SendAAbort(default).ConfigureAwait(false);
                    break;

                case ULConnectionState.Sta5_AwaitingAssociateResponse:
                    if (message.Type == Pdu.Type.AAssociateAc)
                        await AE3_IssueAAssociateConfirmationPrimitive().ConfigureAwait(false);
                    else
                    {
                        // AE-4
                        await _connection.DisposeAsync().ConfigureAwait(false);
                        _connection = null;
                        _state = ULConnectionState.Sta1_Idle;
                    }
                    // TODO Quit waiting for association response
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

        private async ValueTask OnAAbortAsync(ULMessage message)
        {
            ConnectionContext? connection = _connection;
            if (connection != null)
                await connection.DisposeAsync().ConfigureAwait(false);
        }

    }
}
