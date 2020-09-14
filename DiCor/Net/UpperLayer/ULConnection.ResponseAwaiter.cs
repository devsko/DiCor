using System;
using System.Threading;
using System.Threading.Tasks;

namespace DiCor.Net.UpperLayer
{
    partial class ULConnection
    {
        private class ResponseAwaiter : TaskCompletionSource
        {
            private CancellationTokenSource _cts;

            public ResponseAwaiter(ULConnection connection, CancellationToken cancellationToken = default, int timeout = Timeout.Infinite)
                : base(TaskCreationOptions.RunContinuationsAsynchronously)
            {
                if (connection._responseAwaiter is not null)
                    throw new InvalidOperationException();

                connection._responseAwaiter = this;

                _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                if (timeout != Timeout.Infinite)
                {
                    _ = Task.ContinueWith(
                        (task, s) =>
                        {
                            if (!task.IsCanceled)
                                ((CancellationTokenSource)s!).Dispose();
                        },
                        _cts,
                        TaskScheduler.Default);
                    _cts.CancelAfter(timeout);
                }

                this.AttachCancellation(_cts.Token);
            }

            public CancellationToken TimeoutToken
                => _cts.Token;

            public void ResetTimeout(int timeout)
            {
                _cts.CancelAfter(timeout);
            }
        }
    }
}
