using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
    public abstract class ImplementationBase
    {
        public abstract ValueTask ReleaseRequested(ULConnection connection, bool isCollision, CancellationToken cancellationToken = default);
        public abstract ValueTask ReleaseConfirmed(ULConnection connection, CancellationToken cancellationToken = default);
        public abstract ValueTask AbortReceived(ULConnection connection, Pdu.AbortSource source, Pdu.AbortReason reason);
    }
    public abstract class ULScp : ImplementationBase
    {
        private sealed class DefaultScp : ULScp
        {
            public override ValueTask AssociationRequested(ULConnection connection, Association association, CancellationToken cancellationToken)
                => connection.AcceptAssociationAsync(association, cancellationToken);
            public override ValueTask ReleaseRequested(ULConnection connection, bool isCollision, CancellationToken cancellationToken = default)
                => isCollision
                    ? ValueTask.CompletedTask
                    : connection.ConfirmReleaseAsync(cancellationToken);
            public override ValueTask ReleaseConfirmed(ULConnection connection, CancellationToken cancellationToken = default)
                => ValueTask.CompletedTask;
            public override ValueTask AbortReceived(ULConnection connection, Pdu.AbortSource source, Pdu.AbortReason reason)
                => ValueTask.CompletedTask;
        }
        public static ULScp Default { get; } = new DefaultScp();
        public abstract ValueTask AssociationRequested(ULConnection connection, Association association, CancellationToken cancellationToken = default);
    }

    public abstract class ULScu : ImplementationBase
    {
        private sealed class DefaultScu : ULScu
        {
            public override ValueTask AssociationAccepted(ULConnection connection, CancellationToken cancellationToken = default)
                => connection.RequestReleaseAsync(cancellationToken);
            public override ValueTask AssociationRejected(ULConnection connection, Pdu.RejectResult result, Pdu.RejectSource source, Pdu.RejectReason reason, CancellationToken cancellationToken = default)
                => ValueTask.CompletedTask;
            public override ValueTask ReleaseRequested(ULConnection connection, bool isCollision, CancellationToken cancellationToken = default)
                => connection.ConfirmReleaseAsync(cancellationToken);
            public override ValueTask ReleaseConfirmed(ULConnection connection, CancellationToken cancellationToken = default)
                => ValueTask.CompletedTask;
            public override ValueTask AbortReceived(ULConnection connection, Pdu.AbortSource source, Pdu.AbortReason reason)
                => ValueTask.CompletedTask;
        }
        public static ULScu Default { get; } = new DefaultScu();
        public abstract ValueTask AssociationAccepted(ULConnection connection, CancellationToken cancellationToken = default);
        public abstract ValueTask AssociationRejected(ULConnection connection, Pdu.RejectResult result, Pdu.RejectSource source, Pdu.RejectReason reason, CancellationToken cancellationToken = default);
    }

    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "<Pending>")]
    public sealed partial class ULConnection : IAsyncDisposable
    {
        private static readonly TimeSpan s_requestTimeout = TimeSpan.FromMilliseconds(500);
        private static readonly TimeSpan s_rejectTimeout = TimeSpan.FromMilliseconds(500);
        private static readonly TimeSpan s_releaseTimeout = TimeSpan.FromMilliseconds(500);
        private static readonly TimeSpan s_abortTimeout = TimeSpan.FromMilliseconds(500);

        private readonly ILogger _logger;
        private readonly Protocol _protocol;
        private readonly ArtimTimer<ULConnection> _artimTimer;
        private readonly State _state;
        private readonly ImplementationBase _implementation;
        private readonly CancellationTokenSource _lifetime;
        private ConnectionContext? _context;
        private ProtocolReader? _reader;
        private ProtocolWriter? _writer;

        private ULConnection(ImplementationBase implementation, ILoggerFactory? loggerFactory = null)
        {
            bool isProvider = implementation is ULScp;
            _logger = loggerFactory?.CreateLogger($"ULConnection ({(isProvider ? "SCP" : "SCU")})") ?? NullLogger.Instance;
            _protocol = new Protocol(_logger);
            _artimTimer = new ArtimTimer<ULConnection>(OnArtimTimerExpired, this, _logger);
            _state = new State();
            _implementation = implementation;
            _lifetime = new CancellationTokenSource();

            static void OnArtimTimerExpired(ULConnection connection)
            {
                OnArtimTimerExpiredAsync(connection).IgnoreExceptions();

                static async ValueTask OnArtimTimerExpiredAsync(ULConnection connection)
                {
                    using (State.Accessor accessor = await connection._state!.AccessAsync(connection._lifetime!.Token).ConfigureAwait(false))
                    {
                        if (accessor.Current is ConnectionState.Sta2_TransportConnectionOpen or ConnectionState.Sta13_AwaitingTransportConnectionClose)
                        {
                            await connection.AA2_Close(accessor).ConfigureAwait(false);
                        }
                    }
                }
            }
        }

        public static async ValueTask RunScpAsync(ConnectionContext context, ULScp scp, ILoggerFactory? loggerFactory = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(scp);

            ULConnection connection = new(scp, loggerFactory);
            await using (connection.ConfigureAwait(false))
            {
                await connection.RunScpAsync(context, cancellationToken).ConfigureAwait(false);
            }
        }

        private async ValueTask RunScpAsync(ConnectionContext context, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug($"Creating server connection {context.ConnectionId}");

            ValueTask readLoop;
            using (State.Accessor accessor = await _state.AccessAsync(cancellationToken).ConfigureAwait(false))
            {
                _context = context;
                readLoop = RunReadLoopAsync();
                await AE5_AcceptConnection(accessor, cancellationToken).ConfigureAwait(false);
            }

            await readLoop.ConfigureAwait(false);
        }

        public static async ValueTask RunScuAsync(Client client, EndPoint endpoint, AssociationType type, ULScu scu, ILoggerFactory? loggerFactory = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(endpoint);
            if (!Enum.IsDefined(type))
                throw new ArgumentException(null, nameof(type));
            ArgumentNullException.ThrowIfNull(scu);

            ULConnection connection = new(scu, loggerFactory);
            await using (connection.ConfigureAwait(false))
            {
                await connection.RunScuAsync(client, endpoint, new Association(type), cancellationToken).ConfigureAwait(false);
            }
        }

        private async ValueTask RunScuAsync(Client client, EndPoint endpoint, Association association, CancellationToken cancellationToken)
        {
            _logger.LogDebug($"Creating client connection to {endpoint}");

            ValueTask readLoop;
            using (State.Accessor accessor = await _state.AccessAsync(cancellationToken).ConfigureAwait(false))
            {
                await AE1_Connect(client, endpoint, accessor, cancellationToken).ConfigureAwait(false);
                readLoop = RunReadLoopAsync();
                _protocol.Association = association;
                await AE2_SendAAssociateRq(accessor, cancellationToken).ConfigureAwait(false);
            }

            await readLoop.ConfigureAwait(false);
        }

        public bool IsDisposed => _context is null;

        public bool IsProvider => _implementation is ULScp;

        public async ValueTask DisposeAsync()
        {
            ConnectionContext? context = Interlocked.Exchange(ref _context, null);
            if (context is null)
                return;

            _logger.LogTrace("Disposing");

            context.Transport.Input.CancelPendingRead();
            await _lifetime!.CancelAsync().ConfigureAwait(false);
            await context.DisposeAsync().ConfigureAwait(false);

            _logger.LogTrace("Disposed");
        }

        public async ValueTask<ConnectionState> GetCurrentStateAsync()
        {
            using (State.Accessor accessor = await _state.AccessAsync(CancellationToken.None).ConfigureAwait(false))
            {
                return accessor.Current;
            }
        }

        private void SetState(State.Accessor accessor, ConnectionState state)
        {
            _logger.LogTrace($"Change state to {state}");

            accessor.Current = state;
        }

        private async ValueTask RunReadLoopAsync()
        {
            Debug.Assert(_context != null);

            _logger.LogTrace("Starting read loop");

            // TODO _connection.ConnectionClosed
            CancellationToken cancellationToken = _lifetime.Token;

            try
            {
                await using ((_reader = _context.CreateReader()).ConfigureAwait(false))
                await using ((_writer = _context.CreateWriter()).ConfigureAwait(false))
                {
                    while (true)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        ProtocolReadResult<ULMessage> result = await _reader.ReadAsync(_protocol, (int)Association.DefaultMaxDataLength, cancellationToken).ConfigureAwait(false);

                        using (State.Accessor accessor = await _state.AccessAsync(cancellationToken).ConfigureAwait(false))
                        {
                            if (result.IsCompleted || result.IsCanceled)
                            {
                                _logger.LogTrace($"Reader IsCompleted={result.IsCompleted} IsCanceled={result.IsCanceled}");

                                await OnConnectionClosedAsync(accessor).ConfigureAwait(false);
                                break;
                            }

                            _reader.Advance();

                            ULMessage message = result.Message;
                            await (message.Type switch
                            {
                                Pdu.Type.AAssociateRq
                                    => OnAAssociateRqAsync(accessor, cancellationToken),
                                Pdu.Type.AAssociateAc
                                    => OnAAssociateAcAsync(accessor, cancellationToken),
                                Pdu.Type.AAssociateRj
                                    => OnAAssociateRjAsync(message.GetData<AAssociateRjData>(), accessor, cancellationToken),
                                Pdu.Type.PDataTf
                                    => OnPDataTfAsync(message.GetData<PDataTfData>(), accessor, cancellationToken),
                                Pdu.Type.AReleaseRq
                                    => OnAReleaseRqAsync(accessor, cancellationToken),
                                Pdu.Type.AReleaseRp
                                    => OnAReleaseRpAsync(accessor, cancellationToken),
                                Pdu.Type.AAbort
                                    => OnAAbortAsync(message.GetData<AAbortData>(), accessor),
                                _
                                    => OnUnrecognizedPduAsync(accessor, cancellationToken),
                            }).ConfigureAwait(false);
                        }
                    }

                    _logger.LogTrace("Read loop finished");
                }
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
            {
                _logger.LogTrace("Read loop canceled");
            }
        }

        // PS3.8 - 9.2.3 DICOM Upper Layer Protocol for TCP/IP State Transition Table

        public async ValueTask AcceptAssociationAsync(Association association, CancellationToken cancellationToken)
        {
            using (State.Accessor accessor = await _state.AccessAsync(cancellationToken).ConfigureAwait(false))
            {
                if (accessor.Current != ConnectionState.Sta3_AwaitingLocalAssociateResponse)
                {
                    throw new InvalidOperationException();
                }
                _protocol.Association = association;
                await AE7_SendAAssociateAc(accessor, cancellationToken).ConfigureAwait(false);
            }
        }

        public async ValueTask RejectAssociationAsync(Pdu.RejectResult result, Pdu.RejectSource source, Pdu.RejectReason reason, CancellationToken cancellationToken)
        {
            using (State.Accessor accessor = await _state.AccessAsync(cancellationToken).ConfigureAwait(false))
            {
                if (accessor.Current != ConnectionState.Sta3_AwaitingLocalAssociateResponse)
                {
                    throw new InvalidOperationException();
                }
                _protocol.Association = null;
                await AE8_SendAAssociateRj(result, source, reason, accessor, cancellationToken).ConfigureAwait(false);
            }
        }

        public async ValueTask TransferData(CancellationToken cancellationToken = default)
        {
            using (State.Accessor accessor = await _state.AccessAsync(cancellationToken).ConfigureAwait(false))
            {
                await (accessor.Current switch
                {
                    ConnectionState.Sta6_Ready
                        => DT1_SendPDataTf(default, accessor, cancellationToken),
                    ConnectionState.Sta8_AwaitingLocalReleaseResponse
                        => AR7_SendPDataTf(default, accessor, cancellationToken),
                    _
                        => throw new InvalidOperationException(),
                }).ConfigureAwait(false);
            }
        }

        public async ValueTask RequestReleaseAsync(CancellationToken cancellationToken = default)
        {
            using (State.Accessor accessor = await _state.AccessAsync(cancellationToken).ConfigureAwait(false))
            {
                if (accessor.Current != ConnectionState.Sta6_Ready)
                {
                    throw new InvalidOperationException();
                }
                await AR1_SendAReleaseRq(accessor, cancellationToken).ConfigureAwait(false);
            }
        }

        public async ValueTask ConfirmReleaseAsync(CancellationToken cancellationToken = default)
        {
            using (State.Accessor accessor = await _state.AccessAsync(cancellationToken).ConfigureAwait(false))
            {
                await (accessor.Current switch
                {
                    ConnectionState.Sta8_AwaitingLocalReleaseResponse or ConnectionState.Sta12_AwaitingLocalReleaseResponseCollisionScp
                        => AR4_SendAReleaseRp(accessor, cancellationToken),
                    ConnectionState.Sta9_AwaitingLocalReleaseResponseCollisionScu
                        => AR9_SendAReleaseRp(accessor, cancellationToken),
                    _
                        => throw new InvalidOperationException(),
                }).ConfigureAwait(false);
            }
        }

        public async ValueTask AbortAsync(CancellationToken cancellationToken)
        {
            using (State.Accessor accessor = await _state.AccessAsync(cancellationToken).ConfigureAwait(false))
            {
                await (accessor.Current switch
                {
                    ConnectionState.Sta4_AwaitingTransportConnectionOpen
                        => AA2_Close(accessor),
                    ConnectionState.Sta13_AwaitingTransportConnectionClose
                        => throw new InvalidOperationException(),
                    _
                        => AA1_SendAAbort(accessor, cancellationToken),
                }).ConfigureAwait(false);
            }
        }

        private async ValueTask OnConnectionClosedAsync(State.Accessor accessor)
        {
            if (_context == null)
                return;

            _logger.LogDebug($"Connection closed {_context.ConnectionId}");

            await (accessor.Current switch
            {
                ConnectionState.Sta2_TransportConnectionOpen
                    => AA5_StopTimer(accessor),
                ConnectionState.Sta13_AwaitingTransportConnectionClose
                    => AR5_StopTimer(accessor),
                _
                    => AA4_IssueAbortReceived(accessor),
            }).ConfigureAwait(false);
        }

        private async ValueTask OnUnrecognizedPduAsync(State.Accessor accessor, CancellationToken cancellationToken)
            => await (accessor.Current switch
            {
                ConnectionState.Sta2_TransportConnectionOpen
                    => AA1_SendAAbort(accessor, cancellationToken),
                ConnectionState.Sta13_AwaitingTransportConnectionClose
                    => AA7_SendAAbort(accessor, cancellationToken),
                _
                    => AA8_SendAAbortAndIssue(accessor, cancellationToken),
            }).ConfigureAwait(false);

        private async ValueTask OnAAssociateRqAsync(State.Accessor accessor, CancellationToken cancellationToken)
            => await (accessor.Current switch
            {
                ConnectionState.Sta2_TransportConnectionOpen
                    => AE6_IssueAssociationRequested(_protocol.Association!, accessor, cancellationToken),
                ConnectionState.Sta13_AwaitingTransportConnectionClose
                    => AA7_SendAAbort(accessor, cancellationToken),
                _
                    => AA8_SendAAbortAndIssue(accessor, cancellationToken),
            }).ConfigureAwait(false);

        private async ValueTask OnAAssociateAcAsync(State.Accessor accessor, CancellationToken cancellationToken)
            => await (accessor.Current switch
            {
                ConnectionState.Sta2_TransportConnectionOpen
                    => AA1_SendAAbort(accessor, cancellationToken),
                ConnectionState.Sta5_AwaitingAssociateResponse
                    => AE3_IssueAssociationAccepted(accessor, cancellationToken),
                ConnectionState.Sta13_AwaitingTransportConnectionClose
                    => AA6_Ignore(accessor),
                _
                    => AA8_SendAAbortAndIssue(accessor, cancellationToken),
            }).ConfigureAwait(false);

        private async ValueTask OnAAssociateRjAsync(AAssociateRjData data, State.Accessor accessor, CancellationToken cancellationToken)
            => await (accessor.Current switch
            {
                ConnectionState.Sta2_TransportConnectionOpen
                    => AA1_SendAAbort(accessor, cancellationToken),
                ConnectionState.Sta5_AwaitingAssociateResponse
                    => AE4_IssueAssociationRejected(data.Result, data.Source, data.Reason, accessor, cancellationToken),
                ConnectionState.Sta13_AwaitingTransportConnectionClose
                    => AA6_Ignore(accessor),
                _
                    => AA8_SendAAbortAndIssue(accessor, cancellationToken),
            }).ConfigureAwait(false);

        private async ValueTask OnPDataTfAsync(PDataTfData data, State.Accessor accessor, CancellationToken cancellationToken)
            => await (accessor.Current switch
            {
                ConnectionState.Sta2_TransportConnectionOpen
                    => AA1_SendAAbort(accessor, cancellationToken),
                ConnectionState.Sta6_Ready
                    => DT2_IssueDataReceived(data, accessor, cancellationToken),
                ConnectionState.Sta7_AwaitingReleaseResponse
                    => AR6_IssueDataReceived(data, accessor, cancellationToken),
                ConnectionState.Sta13_AwaitingTransportConnectionClose
                    => AA6_Ignore(accessor),
                _
                    => AA8_SendAAbortAndIssue(accessor, cancellationToken),
            }).ConfigureAwait(false);

        private async ValueTask OnAReleaseRqAsync(State.Accessor accessor, CancellationToken cancellationToken)
            => await (accessor.Current switch
            {
                ConnectionState.Sta2_TransportConnectionOpen
                    => AA1_SendAAbort(accessor, cancellationToken),
                ConnectionState.Sta6_Ready
                    => AR2_IssueReleaseRequested(accessor, cancellationToken),
                ConnectionState.Sta7_AwaitingReleaseResponse
                    => AR8_IssueReleaseRequestedCollision(accessor, cancellationToken),
                ConnectionState.Sta13_AwaitingTransportConnectionClose
                    => AA6_Ignore(accessor),
                _
                    => AA8_SendAAbortAndIssue(accessor, cancellationToken),
            }).ConfigureAwait(false);

        private async ValueTask OnAReleaseRpAsync(State.Accessor accessor, CancellationToken cancellationToken)
            => await (accessor.Current switch
            {
                ConnectionState.Sta2_TransportConnectionOpen
                    => AA1_SendAAbort(accessor, cancellationToken),
                ConnectionState.Sta7_AwaitingReleaseResponse or ConnectionState.Sta11_AwaitingReleaseResponseCollisionScu
                    => AR3_IssueReleaseConfirmedAndClose(accessor, cancellationToken),
                ConnectionState.Sta10_AwaitingReleaseResponseCollisionScp
                    => AR10_IssueReleaseConfirmedCollision(accessor, cancellationToken),
                ConnectionState.Sta13_AwaitingTransportConnectionClose
                    => AA6_Ignore(accessor),
                _
                    => AA8_SendAAbortAndIssue(accessor, cancellationToken),
            }).ConfigureAwait(false);

        private async ValueTask OnAAbortAsync(AAbortData data, State.Accessor accessor)
            => await (accessor.Current is ConnectionState.Sta2_TransportConnectionOpen or ConnectionState.Sta13_AwaitingTransportConnectionClose
                ? AA2_Close(accessor)
                : AA3_IssueAbortReceivedAndClose(data.Source, data.Reason, accessor)
            ).ConfigureAwait(false);
    }
}
