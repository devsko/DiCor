using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Threading;

namespace DiCor.Net.UpperLayer
{
    class ResponseAwaiter : TaskCompletionSource
    {
        private CancellationTokenSource? _cts;

        public ResponseAwaiter(CancellationToken cancellationToken = default, int timeout = Timeout.Infinite)
            : base(TaskCreationOptions.RunContinuationsAsynchronously)
        {
            if (timeout != Timeout.Infinite)
            {
                _cts = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    new CancellationTokenSource(timeout).Token);
                cancellationToken = _cts.Token;

                _ = Task.ContinueWith(
                    (_, s) => ((CancellationTokenSource)s!).Dispose(),
                    _cts,
                    TaskScheduler.Default);
            }

            this.AttachCancellation(cancellationToken);
        }

        public void ResetTimeout(int timeout)
        {
            if (_cts == null)
                throw new InvalidOperationException();

            _cts.CancelAfter(timeout);
        }
    }
}
