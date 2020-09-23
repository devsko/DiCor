using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace DiCor.Net.UpperLayer
{
    partial class ULConnection
    {
        private class ResponseAwaiter
        {
            private const int IsCompleted = 0;
            private const int Timedout = 1;
            private const int Unblocked = 2;

#pragma warning disable CA1068 // CancellationToken parameters must come last
            public static async Task<bool> AwaitResponseAsync(ULConnection connection, CancellationToken cancellationToken = default, int timeout = Timeout.Infinite)
#pragma warning restore CA1068 // CancellationToken parameters must come last
            {
                if (connection._responseAwaiter is not null)
                {
                    if (timeout >= 0)
                    {
                        connection._logger.LogDebug($"Reset response awaiter timeout {connection._connection!.ConnectionId}");

                        connection._responseAwaiter?.ResetTimeout(timeout);
                    }

                    return false;
                }

                var awaiter = new ResponseAwaiter(connection, cancellationToken, timeout);

                using (cancellationToken.UnsafeRegister(s => ((ResponseAwaiter)s!).OnCancelled(), awaiter))
                using (awaiter._cts.Token.UnsafeRegister(s => ((ResponseAwaiter)s!).OnTimedOut(), awaiter))
                using (awaiter._cts)
                {
                    connection._logger.LogTrace($"Start waiting for response {(timeout >= 0 ? timeout : "forever")} {connection._connection!.ConnectionId}");

                    int result = await awaiter._tcs.Task.ConfigureAwait(false);

                    connection._logger.LogTrace($"Waiting for response {(result switch { IsCompleted => "completed", Timedout => "timed out", Unblocked => "unblocked", _ => "" })} {connection._connection!.ConnectionId}");

                    return result == IsCompleted;
                }
            }

            private readonly TaskCompletionSource<int> _tcs;
            private readonly CancellationToken _cancellationToken;
            private readonly CancellationTokenSource _cts;

            private ResponseAwaiter(ULConnection connection, CancellationToken cancellationToken, int timeout)
            {
                _tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                _cancellationToken = cancellationToken;
                _cts = new CancellationTokenSource();
                connection._responseAwaiter = this;

                ResetTimeout(timeout);
            }

            public void Completed()
            {
                _tcs.TrySetResult(IsCompleted);
            }

            public void Unblock()
            {
                _tcs.TrySetResult(Unblocked);
            }

            private void OnCancelled()
            {
                _tcs.TrySetCanceled(_cancellationToken);
            }

            private void OnTimedOut()
            {
                _tcs.TrySetResult(Timedout);
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
