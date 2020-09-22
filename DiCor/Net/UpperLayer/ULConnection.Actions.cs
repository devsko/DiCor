using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Bedrock.Framework;

using Microsoft.AspNetCore.Connections;

namespace DiCor.Net.UpperLayer
{
    partial class ULConnection
    {
#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods

        private async ValueTask AE1_Connect(Client client, EndPoint endpoint, CancellationToken cancellationToken)
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

        private Task AE3_ConfirmAAssociateAc(ULMessage<AAssociateAcData> message)
        {
            lock (_lock)
            {
                _association = message.Data.Association!;
                SetState(State.Sta6_Ready);
            }

            AAssociate?.Invoke(this, new AAssociateArgs());

            return Task.CompletedTask;
        }

        private async ValueTask AE4_ConfirmAAssociateRj(ULMessage<AAssociateRjData> message)
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

        private Task AE6_IndicateAssociate(ULMessage<AAssociateRqData> message, CancellationToken cancellationToken)
        {
            if (IsValidAAssociateRq(message))
            {
                lock (_lock)
                {
                    SetState(State.Sta3_AwaitingLocalAssociateResponse);
                }

                AAssociate?.Invoke(this, new AAssociateArgs());

                return Task.CompletedTask;
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

                return AE8_SendAAssociateRj(rjMessage, cancellationToken);
            }
        }

        private ValueTask AE7_SendAAssociateAc(ULMessage<AAssociateAcData> message, CancellationToken cancellationToken)
        {
            ValueTask write;
            lock (_lock)
            {
                write = _writer!.WriteAsync(_protocol, message.ToMessage(), cancellationToken);
                SetState(State.Sta6_Ready);
            }

            return write;
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

        private Task AR5_StopTimer()
        {
            lock (_lock)
            {
                SetState(State.Sta1_Idle);
            }

            return Task.CompletedTask;
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

        private ValueTask AA2_CloseConnection()
        {
            ValueTask dispose;
            lock (_lock)
            {
                dispose = DisposeAsync();
                SetState(State.Sta1_Idle);
            }

            return dispose;
        }

        private async ValueTask AA3_IndicateAbortAndCloseConnection()
        {
            await AA2_CloseConnection().ConfigureAwait(false);

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

        private ValueTask AA7_SendAAbort(CancellationToken cancellationToken)
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

            return write;
        }

        private async Task AA8_SendAndIndicateAAbort(CancellationToken cancellationToken)
        {
            await AA7_SendAAbort(cancellationToken).ConfigureAwait(false);

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
    }
}
