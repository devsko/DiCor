using System;
using System.Threading;
using System.Threading.Tasks;
using DotNext.Threading;

namespace DiCor.Net.UpperLayer
{
    public partial class ULConnection
    {
        // PS3.8 - 9.2.1 Machine States Definition

        public enum ConnectionState
        {
            Sta1_Idle,
            Sta2_TransportConnectionOpen,
            Sta3_AwaitingLocalAssociateResponse,
            Sta4_AwaitingTransportConnectionOpen,
            Sta5_AwaitingAssociateResponse,
            Sta6_Ready,
            Sta7_AwaitingReleaseResponse,
            Sta8_AwaitingLocalReleaseResponse,
            Sta9_AwaitingLocalReleaseResponseCollisionScu,
            Sta10_AwaitingReleaseResponseCollisionScp,
            Sta11_AwaitingReleaseResponseCollisionScu,
            Sta12_AwaitingLocalReleaseResponseCollisionScp,
            Sta13_AwaitingTransportConnectionClose,
        }

        public class State
        {
            private readonly AsyncExclusiveLock _lock = new();
            private ConnectionState _current;

            public async ValueTask<Accessor> AccessAsync(CancellationToken cancellationToken)
            {
                await _lock.AcquireAsync(cancellationToken).ConfigureAwait(false);
                return new Accessor(this);
            }

            public readonly struct Accessor : IDisposable
            {
                private readonly State _state;

                public Accessor(State state)
                {
                    _state = state;
                }

                public ConnectionState Current
                {
                    get => _state._current;
                    set => _state._current = value;
                }

                public Suspension Suspend()
                {
                    _state._lock.Release();
                    return new Suspension(_state);
                }

                public void Dispose()
                {
                    _state._lock.Release();
                }
            }

            public readonly struct Suspension : IAsyncDisposable
            {
                private readonly State _state;
                public Suspension(State state)
                {
                    _state = state;
                }

                public ValueTask DisposeAsync()
                {
                    return _state._lock.AcquireAsync();
                }
            }
        }
    }
}
