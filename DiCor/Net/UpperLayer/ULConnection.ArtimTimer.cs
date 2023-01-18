using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DiCor.Net.UpperLayer
{
    public partial class ULConnection
    {
        public class ArtimTimer<TState>
        {
            private readonly TState _state;
            private readonly Action<TState> _onTimeout;
            private readonly ILogger _logger;
            private CancellationTokenSource? _cts;

            public ArtimTimer(Action<TState> onTimeout, TState state, ILogger? logger = null)
            {
                _state = state;
                _onTimeout = onTimeout;
                _logger = logger ?? NullLogger.Instance;
            }

            public void Start(TimeSpan timeout)
            {
                _logger.LogTrace($"Start waiting for response {(timeout != Timeout.InfiniteTimeSpan ? timeout : "forever")}");

                CancellationTokenSource? old = Volatile.Read(ref _cts);
                CancellationTokenSource? current = old;
                if (current is null)
                {
                    current = new CancellationTokenSource();
                    current = (old = Interlocked.CompareExchange(ref _cts, current, null)) ?? current;
                }
                current.CancelAfter(timeout);

                if (old != current)
                {
                    // Don't call any user code that could need ExecutionContext
                    current.Token.UnsafeRegister(OnTimeout, (this, current));
                }

                static void OnTimeout(object? state)
                {
                    (ArtimTimer<TState> timer, CancellationTokenSource cts) = (ValueTuple<ArtimTimer<TState>, CancellationTokenSource>)state!;
                    timer.OnTimeout(cts);
                }
            }

            private void OnTimeout(CancellationTokenSource cts)
            {
                if (Interlocked.CompareExchange(ref _cts, null, cts) == cts)
                {
                    _logger.LogTrace($"Waiting for response timed out");

                    cts.Dispose();
                    _onTimeout(_state);
                }
            }

            public void Complete()
            {
                _logger.LogTrace($"Waiting for response completed");

                Interlocked.Exchange(ref _cts, null)?.Dispose();
            }
        }
    }
}
