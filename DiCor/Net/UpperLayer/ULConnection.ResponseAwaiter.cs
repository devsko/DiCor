using System;
using System.Threading;
using System.Threading.Tasks;

namespace DiCor.Net.UpperLayer
{
    partial class ULConnection
    {
        private class ResponseAwaiter : TaskCompletionSource
        {
#pragma warning disable CA1068 // CancellationToken parameters must come last
            public static async Task AwaitResponseAsync(ULConnection connection, CancellationToken cancellationToken = default, int timeout = Timeout.Infinite)
#pragma warning restore CA1068 // CancellationToken parameters must come last
            {
                if (connection._responseAwaiter is not null)
                {
                    if (timeout >= 0)
                    {
                        connection._responseAwaiter?.ResetTimeout(timeout);
                    }

                    return;
                }

                var awaiter = new ResponseAwaiter(connection, cancellationToken, timeout);

                using (cancellationToken.UnsafeRegister(s => ((ResponseAwaiter)s!).OnCancelled(), awaiter))
                using (awaiter._cts.Token.UnsafeRegister(s => ((ResponseAwaiter)s!).OnTimedOut(), awaiter))
                using (awaiter._cts)
                {
                    await awaiter.Task.ConfigureAwait(false);
                }

           }

            private readonly CancellationToken _cancellationToken;
            private readonly CancellationTokenSource _cts;

            public bool IsTimedOut { get; private set; }

            private ResponseAwaiter(ULConnection connection, CancellationToken cancellationToken, int timeout)
                : base(TaskCreationOptions.RunContinuationsAsynchronously)
            {
                _cancellationToken = cancellationToken;
                _cts = new CancellationTokenSource();
                connection._responseAwaiter = this;

                ResetTimeout(timeout);
            }

            private void OnCancelled()
            {
                TrySetCanceled(_cancellationToken);
            }

            private void OnTimedOut()
            {
                IsTimedOut = true;
                TrySetCanceled(_cts.Token);
            }

            public void ResetTimeout(int timeout)
            {
                if (timeout >= 0)
                {
                    _cts.CancelAfter(timeout);
                }
            }
        }
    }
}
