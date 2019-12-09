using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bedrock.Framework.Protocols;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;

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

    public class ULConnection
    {
        private readonly ULClient _client;
        private readonly EndPoint _endpoint;
        public Association Association { get; }

        private ULConnectionState _state;
        private ConnectionContext? _connection;
        private ProtocolWriter<ULMessage>? _writer;
        private ProtocolReader<ULMessage>? _reader;


        public ULConnection(ULClient client, EndPoint endpoint, Association association)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            Association = association ?? throw new ArgumentNullException(nameof(association));
            _state = ULConnectionState.Sta1_Idle;
        }

        public async Task AssociateAsync(AssociationType type, CancellationToken cancellationToken = default)
        {
            _connection = await _client.Client.ConnectAsync(_endpoint, cancellationToken).ConfigureAwait(false);

            var protocol = new ULProtocol(this);
            _reader = _connection.CreateReader<ULMessage>(protocol, (int)Association.DefaultMaxDataLength);
            _writer = _connection.CreateWriter<ULMessage>(protocol);

            _ = ReadLoop();

            await _writer.WriteAsync(new ULMessage(Pdu.Type.AAssociateRq), cancellationToken).ConfigureAwait(false);

            _state = ULConnectionState.Sta5_AwaitingAssociateResponse;
        }

        private async Task ReadLoop()
        {
            Debug.Assert(_reader != null);

            while (true)
            {
                ULMessage message = await _reader.ReadAsync();
                if (message.Type == 0)
                    break;

                await ProcessAsync(message).ConfigureAwait(false);
            }
        }

        public bool CanReceive(ULMessage message)
        {
            if (_state == ULConnectionState.Sta1_Idle ||
                _state == ULConnectionState._Sta4_AwaitingTransportConnectionOpen)
                return false;

            return message.Type switch
            {
                Pdu.Type.AAssociateAc => true,
                Pdu.Type.AAssociateRj => true,
                _ => throw new ArgumentException(nameof(message)),
            };
        }

        private ValueTask ProcessAsync(ULMessage message)
            => message.Type switch
            {
                Pdu.Type.AAssociateAc => OnAAssociateResponseAsync(message),
                Pdu.Type.AAssociateRj => OnAAssociateResponseAsync(message),
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
                    await _writer.WriteAsync(
                        new ULMessage(Pdu.Type.AAbort, (byte)Pdu.AbortSource.ServiceUser)
                    ).ConfigureAwait(false);
                    // TODO Start or restart ARTIM
                    _state = ULConnectionState.Sta13_AwaitingTransportConnectionClose;
                    break;

                case ULConnectionState.Sta5_AwaitingAssociateResponse:
                    if (message.Type == Pdu.Type.AAssociateAc)
                    {
                        // AE-3
                        _state = ULConnectionState.Sta6_Ready;
                    }
                    else
                    {
                        // AE-4
                        await _connection.DisposeAsync().ConfigureAwait(false);
                        _connection = null;
                        _state = ULConnectionState.Sta1_Idle;
                    }
                    break;

                case ULConnectionState.Sta13_AwaitingTransportConnectionClose:
                    // AA-6 IGNORE
                    break;

                default:
                    // AA-8
                    await _writer.WriteAsync(
                        new ULMessage(Pdu.Type.AAbort, (byte)Pdu.AbortSource.ServiceProvider)
                    ).ConfigureAwait(false);
                    // TODO Start ARTIM
                    _state = ULConnectionState.Sta13_AwaitingTransportConnectionClose;
                    break;
            }
        }

    }
}
