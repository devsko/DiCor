using DotNext;
using Microsoft.Extensions.Logging;

namespace DiCor.Net.UpperLayer
{
    partial class ULConnection
    {
        private class ArtimTimer<T>
        {
            private readonly T _state;
            private readonly Action<T> _onTimeout;
            private readonly ILogger _logger;
            private CancellationTokenSource? _cts;

            public ArtimTimer(Action<T> onTimeout, T state, ILogger logger)
            {
                _state = state;
                _onTimeout = onTimeout;
                _logger = logger;
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
                    current.Token.UnsafeRegister(OnTimeout, (this, current));
                }

                static void OnTimeout(object? s)
                {
                    (ArtimTimer<T> timer, CancellationTokenSource cts) = (ValueTuple<ArtimTimer<T>, CancellationTokenSource>)s!;
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
