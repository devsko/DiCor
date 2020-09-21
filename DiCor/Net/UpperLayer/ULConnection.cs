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

        private const int AbortTimeout = 500;
        private const int RequestTimeout = 500;
        private const int RejectTimeout = 500;

        public event EventHandler<AAssociateArgs>? AAssociate;
        public event EventHandler<AAbortArgs>? AAbort;
        public event EventHandler<APAbortArgs>? APAbort;

        private readonly Protocol _protocol;
        private readonly object _lock = new();
        private readonly CancellationTokenSource _readLoopCts = new();

        private ConnectionContext? _connection;
        private Association? _association;
        private bool _isServiceProvider;
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
            _isServiceProvider = true;
            _connection = connection;
            _protocol = new(this);
        }

        public State CurrentState => _state;

        public Association? Association => _association;

        private async Task AssociateAsync(Client client, EndPoint endpoint, CancellationToken cancellationToken = default)
        {
            await AE1_Connect(client, endpoint, cancellationToken).ConfigureAwait(false);
            _ = ReadLoopAsync();
            await AE2_SendAAssociateRq(cancellationToken).ConfigureAwait(false);
        }

        private async Task<Task> StartServiceAsync(CancellationToken cancellationToken)
        {
            Task loop = ReadLoopAsync();
            await AE5_AcceptConnection(cancellationToken).ConfigureAwait(false);

            return loop;
        }

        public Task AcceptAssociationAsync(ULMessage<AAssociateAcData> message, CancellationToken cancellationToken)
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
                if (_state == State.Sta4_AwaitingTransportConnectionOpen)
                    return AA2_CloseConnection();
                else
                    return AA1_SendAAbort(cancellationToken);
            }
        }

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

        private async Task AE1_Connect(Client client, EndPoint endpoint, CancellationToken cancellationToken)
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

                var message = new ULMessage<AAssociateRqData>(new() { Association = Association! });
                write = _writer!.WriteAsync(_protocol, message.ToMessage(), cancellationToken);
                SetState(State.Sta5_AwaitingAssociateResponse);
            }
            await write.ConfigureAwait(false);
            await ResponseAwaiter.AwaitResponseAsync(this, cancellationToken).ConfigureAwait(false);
        }

        private Task AE3_ConfirmAAssociate(ULMessage<AAssociateAcData> message)
        {
            lock (_lock)
            {
                _association = message.Data.Association!;
                SetState(State.Sta6_Ready);
            }

            AAssociate?.Invoke(this, new AAssociateArgs());

            return Task.CompletedTask;
        }

        private async Task AE4_ConfirmAAssociate(ULMessage<AAssociateRjData> message)
        {
            ValueTask dispose;
            lock (_lock)
            {
                dispose = DisposeAsync();
                SetState(State.Sta1_Idle);
            }
            await dispose.ConfigureAwait(false);

            AAssociate?.Invoke(this, new AAssociateArgs());
        }

        private async Task AE5_AcceptConnection(CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                if (_state != State.Sta1_Idle)
                    throw new ULProtocolException(State.Sta1_Idle, _state);

                SetState(State.Sta2_TransportConnectionOpen);
            }
            try
            {
                await ResponseAwaiter.AwaitResponseAsync(this, cancellationToken, RequestTimeout).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (_responseAwaiter!.IsTimedOut)
            {
                await AA2_CloseConnection().ConfigureAwait(false);
            }
        }

        private bool IsValidAAssociateRq(ULMessage<AAssociateRqData> message)
            => true;

        private async Task AE6_IndicateAssociate(ULMessage<AAssociateRqData> message, CancellationToken cancellationToken)
        {
            if (IsValidAAssociateRq(message))
            {
                lock (_lock)
                {
                    SetState(State.Sta3_AwaitingLocalAssociateResponse);
                }
                AAssociate?.Invoke(this, new AAssociateArgs());
            }
            else
            {
                var rjMessage = new ULMessage<AAssociateRjData>(new()
                {
                    // TODO
                    Result = Pdu.RejectResult.Permanent,
                    Source = Pdu.RejectSource.ServiceProviderAcse,
                    Reason = Pdu.RejectReason.NoReasonGiven,
                });
                await AE8_SendAAssociateRj(rjMessage, cancellationToken);
            }
        }

        private async Task AE7_SendAAssociateAc(ULMessage<AAssociateAcData> message, CancellationToken cancellationToken)
        {
            ValueTask write;
            lock (_lock)
            {
                write = _writer!.WriteAsync(_protocol, message.ToMessage(), cancellationToken);
                SetState(State.Sta6_Ready);
            }
            await write.ConfigureAwait(false);
        }

        private async Task AE8_SendAAssociateRj(ULMessage<AAssociateRjData> message, CancellationToken cancellationToken)
        {
            ValueTask write;
            lock (_lock)
            {
                write = _writer!.WriteAsync(_protocol, message.ToMessage(), cancellationToken);
                SetState(State.Sta13_AwaitingTransportConnectionClose);
            }
            await write.ConfigureAwait(false);
            try
            {
                await ResponseAwaiter.AwaitResponseAsync(this, cancellationToken, RejectTimeout).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (_responseAwaiter!.IsTimedOut)
            {
                await AA2_CloseConnection().ConfigureAwait(false);
            }
        }

        private async Task AA1_SendAAbort(CancellationToken cancellationToken)
        {
            ValueTask write;
            lock (_lock)
            {
                // TODO Reason
                var message = new ULMessage<AAbortData>(new() { Source = Pdu.AbortSource.ServiceUser, Reason = Pdu.AbortReason.ReasonNotSpecified });
                write = _writer!.WriteAsync(_protocol, message.ToMessage(), cancellationToken);
                SetState(State.Sta13_AwaitingTransportConnectionClose);
            }
            await write.ConfigureAwait(false);
            try
            {
                await ResponseAwaiter.AwaitResponseAsync(this, cancellationToken, AbortTimeout).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (_responseAwaiter!.IsTimedOut)
            {
                await AA2_CloseConnection().ConfigureAwait(false);
            }
        }

        private async Task AA2_CloseConnection()
        {
            ValueTask dispose;
            lock (_lock)
            {
                dispose = DisposeAsync();
                SetState(State.Sta1_Idle);
            }
            await dispose.ConfigureAwait(false);
        }

        private async Task AA3_IndicateAbortAndCloseConnection()
        {
            ValueTask dispose;
            lock (_lock)
            {
                dispose = DisposeAsync();
                SetState(State.Sta1_Idle);
            }
            await dispose.ConfigureAwait(false);

            if (_isServiceProvider)
            {
                AAbort?.Invoke(this, new AAbortArgs());
            }
            else
            {
                APAbort?.Invoke(this, new APAbortArgs());
            }
        }

        private Task AA4_IndicateAbort()
        {
            APAbort?.Invoke(this, new APAbortArgs());

            return Task.CompletedTask;
        }

        private Task AA5_StopTimer()
        {
            lock (_lock)
            {
                SetState(State.Sta1_Idle);
            }

            return Task.CompletedTask;
        }

        private Task AA6_Ignore()
        {
            return Task.CompletedTask;
        }

        private async Task AA7_SendAAbort(CancellationToken cancellationToken)
        {
            ValueTask write;
            lock (_lock)
            {
                if (_state != State.Sta3_AwaitingLocalAssociateResponse)
                    throw new ULProtocolException(State.Sta3_AwaitingLocalAssociateResponse, _state);

                var message = new ULMessage<AAbortData>(new() { Source = Pdu.AbortSource.ServiceProvider, Reason = Pdu.AbortReason.UnexpectedPdu });
                write = _writer!.WriteAsync(_protocol, message.ToMessage(), cancellationToken);
                SetState(State.Sta13_AwaitingTransportConnectionClose);
            }
            await write.ConfigureAwait(false);
        }

        private async Task AA8_SendAndIndicateAAbort(CancellationToken cancellationToken)
        {
            ValueTask write;
            lock (_lock)
            {
                if (_state != State.Sta3_AwaitingLocalAssociateResponse)
                    throw new ULProtocolException(State.Sta3_AwaitingLocalAssociateResponse, _state);

                var message = new ULMessage<AAbortData>(new() { Source = Pdu.AbortSource.ServiceProvider, Reason = Pdu.AbortReason.UnexpectedPdu });
                write = _writer!.WriteAsync(_protocol, message.ToMessage(), cancellationToken);
                SetState(State.Sta13_AwaitingTransportConnectionClose);
            }
            await write.ConfigureAwait(false);

            APAbort?.Invoke(this, new APAbortArgs());

            try
            {
                await ResponseAwaiter.AwaitResponseAsync(this, cancellationToken, AbortTimeout).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (_responseAwaiter!.IsTimedOut)
            {
                await AA2_CloseConnection().ConfigureAwait(false);
            }

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

            CancellationToken cancellationToken = _readLoopCts.Token;
            while (true)
            {
                ProtocolReadResult<ULMessage> result = await _reader.ReadAsync(
                    _protocol,
                    (int)Association.DefaultMaxDataLength,
                    cancellationToken);

                if (result.IsCompleted)
                    break;
                _reader.Advance();

                await ProcessAsync(result.Message, cancellationToken).ConfigureAwait(false);
            }
        }

        private Task ProcessAsync(ULMessage message, CancellationToken cancellationToken)
            => message.Type switch
            {
                Pdu.Type.AAssociateRq
                    => OnAAssociateRqAsync(message.To<AAssociateRqData>(), cancellationToken),
                Pdu.Type.AAssociateAc
                    => OnAAssociateAcAsync(message.To<AAssociateAcData>(), cancellationToken),
                Pdu.Type.AAssociateRj
                    => OnAAssociateRjAsync(message.To<AAssociateRjData>(), cancellationToken),
                Pdu.Type.AAbort
                    => OnAAbortAsync(message.To<AAbortData>(), cancellationToken),

                _ => throw new ArgumentException(null, nameof(message)),
            };

        private Task OnAAssociateRqAsync(ULMessage<AAssociateRqData> message, CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                return _state switch
                {
                    State.Sta2_TransportConnectionOpen
                        => AE6_IndicateAssociate(message, cancellationToken),
                    State.Sta13_AwaitingTransportConnectionClose
                        => AA7_SendAAbort(cancellationToken),

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
                        => AE3_ConfirmAAssociate(message),
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
                        => AE4_ConfirmAAssociate(message),
                    State.Sta13_AwaitingTransportConnectionClose
                        => AA6_Ignore(),

                    _ => AA8_SendAndIndicateAAbort(cancellationToken),
                };
            }
        }

        private Task OnAAbortAsync(ULMessage<AAbortData> message, CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                return _state switch
                {
                    State.Sta2_TransportConnectionOpen or
                    State.Sta13_AwaitingTransportConnectionClose =>
                        AA2_CloseConnection(),

                    _ => AA3_IndicateAbortAndCloseConnection(),
                };
            }
        }

    }
}
