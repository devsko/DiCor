#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable CA1822 // Mark members as static

using System.Diagnostics;
using System.Net;

using Bedrock.Framework;

namespace DiCor.Net.UpperLayer
{
    partial class ULConnection
    {
        private async Task AE1_Connect(Client client, EndPoint endpoint, State.Accessor accessor, CancellationToken cancellationToken)
        {
            _context = await client.ConnectAsync(endpoint, cancellationToken).ConfigureAwait(false);

            SetState(accessor, ConnectionState.Sta4_AwaitingTransportConnectionOpen);
        }

        private async Task AE2_SendAAssociateRq(State.Accessor accessor, CancellationToken cancellationToken)
        {
            await _writer!.WriteAsync(_protocol, new ULMessage(Pdu.Type.AAssociateRq), cancellationToken).ConfigureAwait(false);

            SetState(accessor, ConnectionState.Sta5_AwaitingAssociateResponse);
        }

        private async Task AE3_ConfirmAAssociateAc(State.Accessor accessor, CancellationToken cancellationToken)
        {
            SetState(accessor, ConnectionState.Sta6_Ready);

            await using (accessor.Suspend().ConfigureAwait(false))
            {
                await Scu.AAssociateAccepted(this, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task AE4_ConfirmAAssociateRj(Pdu.RejectResult result, Pdu.RejectSource source, Pdu.RejectReason reason, State.Accessor accessor, CancellationToken cancellationToken)
        {
            SetState(accessor, ConnectionState.Sta1_Idle);

            await using (accessor.Suspend().ConfigureAwait(false))
            {
                await Scu.AAssociateRejected(this, result, source, reason, cancellationToken).ConfigureAwait(false);
            }

            await DisposeAsync().ConfigureAwait(false);
        }

        private async Task AE5_AcceptConnection(State.Accessor accessor, CancellationToken cancellationToken)
        {
            _artimTimer.Start(s_requestTimeout);

            SetState(accessor, ConnectionState.Sta2_TransportConnectionOpen);
        }

        private bool IsValid(Association association)
            => true;

        private async Task AE6_IndicateAssociate(Association association, State.Accessor accessor, CancellationToken cancellationToken)
        {
            _artimTimer.Complete();

            if (IsValid(association))
            {
                SetState(accessor, ConnectionState.Sta3_AwaitingLocalAssociateResponse);

                await using (accessor.Suspend().ConfigureAwait(false))
                {
                    await Scp.AAssociateRequested(this, association, cancellationToken).ConfigureAwait(false);
                }
            }

            await AE8_SendAAssociateRj(Pdu.RejectResult.Permanent, Pdu.RejectSource.ServiceProviderAcse, Pdu.RejectReason.NoReasonGiven, accessor, cancellationToken).ConfigureAwait(false);
        }

        private async Task AE7_SendAAssociateAc(State.Accessor accessor, CancellationToken cancellationToken)
        {
            await _writer!.WriteAsync(_protocol, new ULMessage(Pdu.Type.AAssociateAc), cancellationToken).ConfigureAwait(false);

            SetState(accessor, ConnectionState.Sta6_Ready);
        }

        private async Task AE8_SendAAssociateRj(Pdu.RejectResult result, Pdu.RejectSource source, Pdu.RejectReason reason, State.Accessor accessor, CancellationToken cancellationToken)
        {
            AAssociateRjData data = new()
            {
                Result = result,
                Source = source,
                Reason = reason,
            };
            await _writer!.WriteAsync(_protocol, ULMessage.FromData(data), cancellationToken).ConfigureAwait(false);
            _artimTimer.Start(s_rejectTimeout);

            SetState(accessor, ConnectionState.Sta13_AwaitingTransportConnectionClose);
        }

        private async Task AR1_SendAReleaseRq(State.Accessor accessor, CancellationToken cancellationToken)
        {
            await _writer!.WriteAsync(_protocol, new ULMessage(Pdu.Type.AReleaseRq), cancellationToken).ConfigureAwait(false);

            SetState(accessor, ConnectionState.Sta7_AwaitingReleaseResponse);
        }

        private async Task AR2_IndicateRelease(State.Accessor accessor, CancellationToken cancellationToken)
        {
            SetState(accessor, ConnectionState.Sta8_AwaitingLocalReleaseResponse);

            await using (accessor.Suspend().ConfigureAwait(false))
            {
                if (IsProvider)
                {
                    await Scp.AReleaseRequested(this, isCollision: false, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await Scu.AReleaseRequested(this, isCollision: false, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private async Task AR3_ConfirmReleaseAndCloseConnection(State.Accessor accessor, CancellationToken cancellationToken)
        {
            SetState(accessor, ConnectionState.Sta1_Idle);

            await using (accessor.Suspend().ConfigureAwait(false))
            {
                if (IsProvider)
                {
                    await Scp.AReleaseConfirmed(this, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await Scu.AReleaseConfirmed(this, cancellationToken).ConfigureAwait(false);
                }
            }

            await DisposeAsync().ConfigureAwait(false);
        }

        private async Task AR4_SendAReleaseRp(State.Accessor accessor, CancellationToken cancellationToken)
        {
            await _writer!.WriteAsync(_protocol, new ULMessage(Pdu.Type.AReleaseRp), cancellationToken).ConfigureAwait(false);
            _artimTimer.Start(s_releaseTimeout);

            SetState(accessor, ConnectionState.Sta13_AwaitingTransportConnectionClose);
        }

        private async Task AR5_StopTimer(State.Accessor accessor)
        {
            _artimTimer.Complete();

            SetState(accessor, ConnectionState.Sta1_Idle);
        }

        private async Task AR8_IndicateReleaseCollision(State.Accessor accessor, CancellationToken cancellationToken)
        {
            SetState(accessor, IsProvider
                ? ConnectionState.Sta10_AwaitingReleaseResponseCollisionScp
                : ConnectionState.Sta9_AwaitingLocalReleaseResponseCollisionScu);

            await using (accessor.Suspend().ConfigureAwait(false))
            {
                if (IsProvider)
                {
                    await Scp.AReleaseRequested(this, isCollision: true, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await Scu.AReleaseRequested(this, isCollision: true, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private async Task AR9_SendAReleaseRp(State.Accessor accessor, CancellationToken cancellationToken)
        {
            await _writer!.WriteAsync(_protocol, new ULMessage(Pdu.Type.AReleaseRp), cancellationToken).ConfigureAwait(false);

            SetState(accessor, ConnectionState.Sta11_AwaitingReleaseResponseCollisionScu);
        }

        private async Task AR10_ConfirmReleaseCollision(State.Accessor accessor, CancellationToken cancellationToken)
        {
            SetState(accessor, ConnectionState.Sta12_AwaitingLocalReleaseResponseCollisionScp);

            await using (accessor.Suspend().ConfigureAwait(false))
            {
                await Scp.AReleaseConfirmed(this, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task AA1_SendAAbort(State.Accessor accessor, CancellationToken cancellationToken)
        {
            // TODO Reason
            var data = new AAbortData
            {
                Source = Pdu.AbortSource.ServiceUser,
                Reason = Pdu.AbortReason.ReasonNotSpecified
            };
            await _writer!.WriteAsync(_protocol, ULMessage.FromData(data), cancellationToken).ConfigureAwait(false);
            _artimTimer.Start(s_abortTimeout);

            SetState(accessor, ConnectionState.Sta13_AwaitingTransportConnectionClose);
        }

        private async Task AA2_CloseConnection(State.Accessor accessor)
        {
            _artimTimer.Complete();

            SetState(accessor, ConnectionState.Sta1_Idle);
            await DisposeAsync().ConfigureAwait(false);
        }

        private async Task AA3_IndicateAbortAndCloseConnection(Pdu.AbortSource source, Pdu.AbortReason reason, State.Accessor accessor)
        {
            SetState(accessor, ConnectionState.Sta1_Idle);

            await using (accessor.Suspend().ConfigureAwait(false))
            {
                if (IsProvider)
                {
                    await Scp.AAbort(this, source, reason).ConfigureAwait(false);
                }
                else
                {
                    await Scu.APAbort(this, source, reason).ConfigureAwait(false);
                }
            }

            await DisposeAsync().ConfigureAwait(false);
        }

        private async Task AA4_IndicateAbort(State.Accessor accessor)
        {
            SetState(accessor, ConnectionState.Sta1_Idle);

            await using (accessor.Suspend().ConfigureAwait(false))
            {
                await Scu.APAbort(this, Pdu.AbortSource.ServiceProvider, Pdu.AbortReason.ReasonNotSpecified).ConfigureAwait(false);
            }
        }

        private Task AA5_StopTimer(State.Accessor accessor)
        {
            _artimTimer.Complete();
            SetState(accessor, ConnectionState.Sta1_Idle);

            return Task.CompletedTask;
        }

        private async Task AA6_Ignore(State.Accessor accessor)
        { }

        private async Task AA7_SendAAbort(State.Accessor accessor, CancellationToken cancellationToken)
        {
            AAbortData data = new() { Source = Pdu.AbortSource.ServiceProvider, Reason = Pdu.AbortReason.UnexpectedPdu };
            await _writer!.WriteAsync(_protocol, ULMessage.FromData(data), cancellationToken).ConfigureAwait(false);

            SetState(accessor, ConnectionState.Sta13_AwaitingTransportConnectionClose);
        }

        private async Task AA8_SendAndIndicateAAbort(State.Accessor accessor, CancellationToken cancellationToken)
        {
            AAbortData data = new() { Source = Pdu.AbortSource.ServiceProvider, Reason = Pdu.AbortReason.UnexpectedPdu };
            await _writer!.WriteAsync(_protocol, ULMessage.FromData(data), cancellationToken).ConfigureAwait(false);

            SetState(accessor, ConnectionState.Sta13_AwaitingTransportConnectionClose);
            _artimTimer.Start(s_abortTimeout);

            await using (accessor.Suspend().ConfigureAwait(false))
            {
                await Scu.APAbort(this, Pdu.AbortSource.ServiceProvider, Pdu.AbortReason.UnrecognizedPdu).ConfigureAwait(false);
            }
        }
    }
}

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore CA1822 // Mark members as static
