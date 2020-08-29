using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Connections;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bedrock.Framework.Protocols;
using Microsoft.AspNetCore.Connections;

namespace DiCor.Net.UpperLayer
{
    public enum ULConnectionState
    {
        Sta1_Idle,
        Sta2_TransportConnectionOpen,
        Sta3_AwaitingLocalAssociateResponse,
        _Sta4_AwaitingTransportConnectionOpen,
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

    public class ULConnection : ICancellationNotification
    {
        private readonly ULClient _client;
        private readonly EndPoint _endpoint;
        private Association? _association;

        private ULConnectionState _state;
        private Connection? _connection;
        private readonly ULProtocol _protocol;
        private ProtocolWriter? _writer;
        private ProtocolReader? _reader;

        private TaskCompletionSource<Association?>? _associationTcs;

        public ULConnection(ULClient client, EndPoint endpoint)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            _state = ULConnectionState.Sta1_Idle;
            _protocol = new ULProtocol(this);
        }

        public ULConnectionState State => _state;

        public Association? Association => _association;

        public async Task<Association?> AssociateAsync(AssociationType type, CancellationToken cancellationToken = default)
        {
            if (_state != ULConnectionState.Sta1_Idle)
                // TODO ProtocolException
                throw new InvalidOperationException();

            _association = new Association(type);
            _associationTcs = new TaskCompletionSource<Association?>(TaskCreationOptions.RunContinuationsAsynchronously);
            _associationTcs.AttachCancellation(cancellationToken, this);

            // AE-1
            _connection = await _client.Client.ConnectAsync(_endpoint, cancellationToken: cancellationToken).ConfigureAwait(false);

            _state = ULConnectionState._Sta4_AwaitingTransportConnectionOpen;

            _reader = _connection.CreateReader();
            _writer = _connection.CreateWriter();

            _ = ReadLoopAsync(cancellationToken);

            // AE-2
            await _writer.WriteAsync(_protocol, new ULMessage(Pdu.Type.AAssociateRq), cancellationToken).ConfigureAwait(false);

            _state = ULConnectionState.Sta5_AwaitingAssociateResponse;

            return await _associationTcs.Task.ConfigureAwait(false);
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

        public bool CanReceive(ULMessage message)
        {
            if (_state == ULConnectionState.Sta1_Idle ||
                _state == ULConnectionState._Sta4_AwaitingTransportConnectionOpen)
                return false;

            return message.Type switch
            {
                Pdu.Type.AAssociateAc => _state == ULConnectionState.Sta5_AwaitingAssociateResponse,
                Pdu.Type.AAssociateRj => _state == ULConnectionState.Sta5_AwaitingAssociateResponse,
                Pdu.Type.AAbort => true,
                _ => throw new ArgumentException(nameof(message)),
            };
        }

        private ValueTask ProcessAsync(ULMessage message)
            => message.Type switch
            {
                Pdu.Type.AAssociateAc => OnAAssociateResponseAsync(message),
                Pdu.Type.AAssociateRj => OnAAssociateResponseAsync(message),
                Pdu.Type.AAbort => OnAAbortAsync(message),
                _ => throw new ArgumentException(nameof(message)),
            };

        private async ValueTask OnAAssociateResponseAsync(ULMessage message)
        {
            Debug.Assert(_connection != null);
            Debug.Assert(_writer != null);

            switch (_state)
            {
                case ULConnectionState.Sta1_Idle:
                case ULConnectionState._Sta4_AwaitingTransportConnectionOpen:
                    // CANNOT RECEIVE WITHOUT CONNECTION
                    throw new InvalidOperationException("How did we receive a message without connection?");

                case ULConnectionState.Sta2_TransportConnectionOpen:
                    // AA-1
                    await _writer.WriteAsync(_protocol, new ULMessage(Pdu.Type.AAbort, (byte)Pdu.AbortSource.ServiceUser)).ConfigureAwait(false);
                    // TODO Start or restart ARTIM
                    _state = ULConnectionState.Sta13_AwaitingTransportConnectionClose;
                    break;

                case ULConnectionState.Sta5_AwaitingAssociateResponse:
                    if (message.Type == Pdu.Type.AAssociateAc)
                        // AE-3
                        _state = ULConnectionState.Sta6_Ready;
                    else
                    {
                        // AE-4
                        await _connection.DisposeAsync().ConfigureAwait(false);
                        _connection = null;
                        _association = null;
                        _state = ULConnectionState.Sta1_Idle;
                    }
                    _associationTcs!.TrySetResult(_association);
                    _associationTcs = null;
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

            Connection? connection = _connection;
            if (connection != null)
                await connection.DisposeAsync().ConfigureAwait(false);
        }

        public void OnCanceled()
        {
            //if (_connection != null)
        }
    }
}
