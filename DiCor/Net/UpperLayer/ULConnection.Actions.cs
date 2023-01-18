#pragma warning disable CA1822 // Mark members as static
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning disable IDE0060 // Remove unused parameter

using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Bedrock.Framework;

namespace DiCor.Net.UpperLayer
{
    public partial class ULConnection
    {
        // PS3.8 - 9.2.2 State Machine Actions Definition

        private async ValueTask AE1_Connect(Client client, EndPoint endpoint, State.Accessor accessor, CancellationToken cancellationToken)
        {
            _context = await client.ConnectAsync(endpoint, cancellationToken).ConfigureAwait(false);

            SetState(accessor, ConnectionState.Sta4_AwaitingTransportConnectionOpen);
        }

        private async ValueTask AE2_SendAAssociateRq(State.Accessor accessor, CancellationToken cancellationToken)
        {
            await _writer!.WriteAsync(_protocol, new ULMessage(Pdu.Type.AAssociateRq), cancellationToken).ConfigureAwait(false);

            SetState(accessor, ConnectionState.Sta5_AwaitingAssociateResponse);
        }

        private async ValueTask AE3_IssueAssociationAccepted(State.Accessor accessor, CancellationToken cancellationToken)
        {
            SetState(accessor, ConnectionState.Sta6_Ready);

            await using (accessor.Suspend().ConfigureAwait(false))
            {
                await ((ULScu)_implementation).AssociationAccepted(this, cancellationToken).ConfigureAwait(false);
            }
        }

        private async ValueTask AE4_IssueAssociationRejected(Pdu.RejectResult result, Pdu.RejectSource source, Pdu.RejectReason reason, State.Accessor accessor, CancellationToken cancellationToken)
        {
            SetState(accessor, ConnectionState.Sta1_Idle);

            await using (accessor.Suspend().ConfigureAwait(false))
            {
                await ((ULScu)_implementation).AssociationRejected(this, result, source, reason, cancellationToken).ConfigureAwait(false);
            }

            await DisposeAsync().ConfigureAwait(false);
        }

        private async ValueTask AE5_AcceptConnection(State.Accessor accessor, CancellationToken cancellationToken)
        {
            _artimTimer.Start(s_requestTimeout);

            SetState(accessor, ConnectionState.Sta2_TransportConnectionOpen);
        }

        private bool IsValid(Association association)
            => true;

        private async ValueTask AE6_IssueAssociationRequested(Association association, State.Accessor accessor, CancellationToken cancellationToken)
        {
            _artimTimer.Complete();

            if (IsValid(association))
            {
                SetState(accessor, ConnectionState.Sta3_AwaitingLocalAssociateResponse);

                await using (accessor.Suspend().ConfigureAwait(false))
                {
                    await ((ULScp)_implementation).AssociationRequested(this, association, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                await AE8_SendAAssociateRj(Pdu.RejectResult.Permanent, Pdu.RejectSource.ServiceProviderAcse, Pdu.RejectReason.NoReasonGiven, accessor, cancellationToken).ConfigureAwait(false);
            }
        }

        private async ValueTask AE7_SendAAssociateAc(State.Accessor accessor, CancellationToken cancellationToken)
        {
            await _writer!.WriteAsync(_protocol, new ULMessage(Pdu.Type.AAssociateAc), cancellationToken).ConfigureAwait(false);

            SetState(accessor, ConnectionState.Sta6_Ready);
        }

        private async ValueTask AE8_SendAAssociateRj(Pdu.RejectResult result, Pdu.RejectSource source, Pdu.RejectReason reason, State.Accessor accessor, CancellationToken cancellationToken)
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

        private async ValueTask DT1_SendPDataTf(PDataTfData data, State.Accessor accessor, CancellationToken cancellationToken)
        {

            SetState(accessor, ConnectionState.Sta6_Ready);
        }

        private async ValueTask DT2_IssueDataReceived(PDataTfData data, State.Accessor accessor, CancellationToken cancellationToken)
        {
            SetState(accessor, ConnectionState.Sta6_Ready);

        }

        private async ValueTask AR1_SendAReleaseRq(State.Accessor accessor, CancellationToken cancellationToken)
        {
            await _writer!.WriteAsync(_protocol, new ULMessage(Pdu.Type.AReleaseRq), cancellationToken).ConfigureAwait(false);

            SetState(accessor, ConnectionState.Sta7_AwaitingReleaseResponse);
        }

        private async ValueTask AR2_IssueReleaseRequested(State.Accessor accessor, CancellationToken cancellationToken)
        {
            SetState(accessor, ConnectionState.Sta8_AwaitingLocalReleaseResponse);

            await using (accessor.Suspend().ConfigureAwait(false))
            {
                await _implementation.ReleaseRequested(this, isCollision: false, cancellationToken).ConfigureAwait(false);
            }
        }

        private async ValueTask AR3_IssueReleaseConfirmedAndClose(State.Accessor accessor, CancellationToken cancellationToken)
        {
            SetState(accessor, ConnectionState.Sta1_Idle);

            await using (accessor.Suspend().ConfigureAwait(false))
            {
                await _implementation.ReleaseConfirmed(this, cancellationToken).ConfigureAwait(false);
            }

            await DisposeAsync().ConfigureAwait(false);
        }

        private async ValueTask AR4_SendAReleaseRp(State.Accessor accessor, CancellationToken cancellationToken)
        {
            await _writer!.WriteAsync(_protocol, new ULMessage(Pdu.Type.AReleaseRp), cancellationToken).ConfigureAwait(false);
            _artimTimer.Start(s_releaseTimeout);

            SetState(accessor, ConnectionState.Sta13_AwaitingTransportConnectionClose);
        }

        private async ValueTask AR5_StopTimer(State.Accessor accessor)
        {
            _artimTimer.Complete();

            SetState(accessor, ConnectionState.Sta1_Idle);
        }

        private async ValueTask AR6_IssueDataReceived(PDataTfData data, State.Accessor accessor, CancellationToken cancellationToken)
        {
            SetState(accessor, ConnectionState.Sta7_AwaitingReleaseResponse);

        }

        private async ValueTask AR7_SendPDataTf(PDataTfData data, State.Accessor accessor, CancellationToken cancellationToken)
        {

            SetState(accessor, ConnectionState.Sta8_AwaitingLocalReleaseResponse);
        }

        private async ValueTask AR8_IssueReleaseRequestedCollision(State.Accessor accessor, CancellationToken cancellationToken)
        {
            SetState(accessor, IsProvider
                ? ConnectionState.Sta10_AwaitingReleaseResponseCollisionScp
                : ConnectionState.Sta9_AwaitingLocalReleaseResponseCollisionScu);

            await using (accessor.Suspend().ConfigureAwait(false))
            {
                await _implementation.ReleaseRequested(this, isCollision: true, cancellationToken).ConfigureAwait(false);
            }
        }

        private async ValueTask AR9_SendAReleaseRp(State.Accessor accessor, CancellationToken cancellationToken)
        {
            await _writer!.WriteAsync(_protocol, new ULMessage(Pdu.Type.AReleaseRp), cancellationToken).ConfigureAwait(false);

            SetState(accessor, ConnectionState.Sta11_AwaitingReleaseResponseCollisionScu);
        }

        private async ValueTask AR10_IssueReleaseConfirmedCollision(State.Accessor accessor, CancellationToken cancellationToken)
        {
            SetState(accessor, ConnectionState.Sta12_AwaitingLocalReleaseResponseCollisionScp);

            await using (accessor.Suspend().ConfigureAwait(false))
            {
                await _implementation.ReleaseConfirmed(this, cancellationToken).ConfigureAwait(false);
            }
        }

        private async ValueTask AA1_SendAAbort(State.Accessor accessor, CancellationToken cancellationToken)
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

        private async ValueTask AA2_Close(State.Accessor accessor)
        {
            _artimTimer.Complete();

            SetState(accessor, ConnectionState.Sta1_Idle);
            await DisposeAsync().ConfigureAwait(false);
        }

        private async ValueTask AA3_IssueAbortReceivedAndClose(Pdu.AbortSource source, Pdu.AbortReason reason, State.Accessor accessor)
        {
            SetState(accessor, ConnectionState.Sta1_Idle);

            await using (accessor.Suspend().ConfigureAwait(false))
            {
                await _implementation.AbortReceived(this, source, reason).ConfigureAwait(false);
            }

            await DisposeAsync().ConfigureAwait(false);
        }

        private async ValueTask AA4_IssueAbortReceived(State.Accessor accessor)
        {
            SetState(accessor, ConnectionState.Sta1_Idle);

            await using (accessor.Suspend().ConfigureAwait(false))
            {
                await _implementation.AbortReceived(this, Pdu.AbortSource.ServiceProvider, Pdu.AbortReason.ReasonNotSpecified).ConfigureAwait(false);
            }
        }

        private ValueTask AA5_StopTimer(State.Accessor accessor)
        {
            _artimTimer.Complete();
            SetState(accessor, ConnectionState.Sta1_Idle);

            return ValueTask.CompletedTask;
        }

        private async ValueTask AA6_Ignore(State.Accessor accessor)
        { }

        private async ValueTask AA7_SendAAbort(State.Accessor accessor, CancellationToken cancellationToken)
        {
            AAbortData data = new() { Source = Pdu.AbortSource.ServiceProvider, Reason = Pdu.AbortReason.UnexpectedPdu };
            await _writer!.WriteAsync(_protocol, ULMessage.FromData(data), cancellationToken).ConfigureAwait(false);

            SetState(accessor, ConnectionState.Sta13_AwaitingTransportConnectionClose);
        }

        private async ValueTask AA8_SendAAbortAndIssue(State.Accessor accessor, CancellationToken cancellationToken)
        {
            await AA7_SendAAbort(accessor, cancellationToken).ConfigureAwait(false);

            _artimTimer.Start(s_abortTimeout);

            await using (accessor.Suspend().ConfigureAwait(false))
            {
                await _implementation.AbortReceived(this, Pdu.AbortSource.ServiceProvider, Pdu.AbortReason.UnrecognizedPdu).ConfigureAwait(false);
            }
        }
    }
}

#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning restore CA1822 // Mark members as static
