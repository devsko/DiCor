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
        public ResponseAwaiter(CancellationToken cancellationToken = default, int timeout = Timeout.Infinite)
            : base(TaskCreationOptions.RunContinuationsAsynchronously)
        {
            if (timeout != Timeout.Infinite)
            {
                var cts = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    new CancellationTokenSource(timeout).Token);
                cancellationToken = cts.Token;

                _ = Task.ContinueWith(
                    (_, s) => ((CancellationTokenSource?)s)?.Dispose(),
                    cts,
                    TaskScheduler.Default);
            }

            this.AttachCancellation(cancellationToken);
        }
    }
}
