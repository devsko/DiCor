using System;
using System.Threading;
using System.Threading.Tasks;

namespace DiCor
{
    //public interface ICancellationNotification
    //{
    //    void OnCanceled();
    //}

    public static class ThreadingExtensions
    {
        public static void IgnoreExceptions(ValueTask<int> task)
        {
            _ = IgnoreExceptionsAsync(task);

            static async Task IgnoreExceptionsAsync(ValueTask<int> task)
            {
#pragma warning disable ERP022 // Unobserved exception in generic exception handler
                try { await task.ConfigureAwait(false); } catch { }
#pragma warning restore ERP022 // Unobserved exception in generic exception handler
            }
        }

        /// <summary>Awaits a task, ignoring any resulting exceptions.</summary>
        public static void IgnoreExceptions(Task task)
        {
            _ = IgnoreExceptionsAsync(task);

            static async Task IgnoreExceptionsAsync(Task task)
            {
#pragma warning disable ERP022 // Unobserved exception in generic exception handler
                try { await task.ConfigureAwait(false); } catch { }
#pragma warning restore ERP022 // Unobserved exception in generic exception handler
            }
        }

        ///// <summary>
        ///// A state object for tracking cancellation and a TaskCompletionSource.
        ///// </summary>
        ///// <typeparam name="T">The type of value returned from a task.</typeparam>
        ///// <remarks>
        ///// We use this class so that we only allocate one object to support all continuations
        ///// required for cancellation handling, rather than a special closure and delegate for each one.
        ///// </remarks>
        //private class CancelableTaskCompletionSource<T>
        //{
        //    /// <summary>
        //    /// The ID of the thread on which this instance was created.
        //    /// </summary>
        //    private readonly int _ownerThreadId = Environment.CurrentManagedThreadId;

        //    /// <summary>
        //    /// Initializes a new instance of the <see cref="CancelableTaskCompletionSource{T}"/> class.
        //    /// </summary>
        //    /// <param name="taskCompletionSource">The task completion source.</param>
        //    /// <param name="cancellationCallback">A callback to invoke when cancellation occurs.</param>
        //    /// <param name="cancellationToken">The cancellation token.</param>
        //    internal CancelableTaskCompletionSource(TaskCompletionSource<T> taskCompletionSource, ICancellationNotification? cancellationCallback, CancellationToken cancellationToken)
        //    {
        //        TaskCompletionSource = taskCompletionSource ?? throw new ArgumentNullException(nameof(taskCompletionSource));
        //        CancellationToken = cancellationToken;
        //        CancellationCallback = cancellationCallback;
        //    }

        //    /// <summary>
        //    /// Gets the cancellation token.
        //    /// </summary>
        //    internal CancellationToken CancellationToken { get; }

        //    /// <summary>
        //    /// Gets the Task completion source.
        //    /// </summary>
        //    internal TaskCompletionSource<T> TaskCompletionSource { get; }

        //    internal ICancellationNotification? CancellationCallback { get; }

        //    /// <summary>
        //    /// Gets or sets the cancellation token registration.
        //    /// </summary>
        //    internal CancellationTokenRegistration CancellationTokenRegistration { get; set; }

        //    /// <summary>
        //    /// Gets or sets a value indicating whether the continuation has been scheduled (and not run inline).
        //    /// </summary>
        //    internal bool ContinuationScheduled { get; set; }

        //    /// <summary>
        //    /// Gets a value indicating whether the caller is on the same thread as the one that created this instance.
        //    /// </summary>
        //    internal bool OnOwnerThread => Environment.CurrentManagedThreadId == _ownerThreadId;
        //}

        ///// <summary>
        ///// A state object for tracking cancellation and a TaskCompletionSource.
        ///// </summary>
        ///// <remarks>
        ///// We use this class so that we only allocate one object to support all continuations
        ///// required for cancellation handling, rather than a special closure and delegate for each one.
        ///// </remarks>
        //private class CancelableTaskCompletionSource
        //{
        //    /// <summary>
        //    /// The ID of the thread on which this instance was created.
        //    /// </summary>
        //    private readonly int _ownerThreadId = Environment.CurrentManagedThreadId;

        //    /// <summary>
        //    /// Initializes a new instance of the <see cref="CancelableTaskCompletionSource"/> class.
        //    /// </summary>
        //    /// <param name="taskCompletionSource">The task completion source.</param>
        //    /// <param name="cancellationCallback">A callback to invoke when cancellation occurs.</param>
        //    /// <param name="cancellationToken">The cancellation token.</param>
        //    internal CancelableTaskCompletionSource(TaskCompletionSource taskCompletionSource, ICancellationNotification? cancellationCallback, CancellationToken cancellationToken)
        //    {
        //        TaskCompletionSource = taskCompletionSource ?? throw new ArgumentNullException(nameof(taskCompletionSource));
        //        CancellationToken = cancellationToken;
        //        CancellationCallback = cancellationCallback;
        //    }

        //    /// <summary>
        //    /// Gets the cancellation token.
        //    /// </summary>
        //    internal CancellationToken CancellationToken { get; }

        //    /// <summary>
        //    /// Gets the Task completion source.
        //    /// </summary>
        //    internal TaskCompletionSource TaskCompletionSource { get; }

        //    internal ICancellationNotification? CancellationCallback { get; }

        //    /// <summary>
        //    /// Gets or sets the cancellation token registration.
        //    /// </summary>
        //    internal CancellationTokenRegistration CancellationTokenRegistration { get; set; }

        //    /// <summary>
        //    /// Gets or sets a value indicating whether the continuation has been scheduled (and not run inline).
        //    /// </summary>
        //    internal bool ContinuationScheduled { get; set; }

        //    /// <summary>
        //    /// Gets a value indicating whether the caller is on the same thread as the one that created this instance.
        //    /// </summary>
        //    internal bool OnOwnerThread => Environment.CurrentManagedThreadId == _ownerThreadId;
        //}

//        /// <summary>
//        /// Cancels a <see cref="TaskCompletionSource{TResult}.Task"/> if a given <see cref="CancellationToken"/> is canceled.
//        /// </summary>
//        /// <typeparam name="T">The type of value returned by a successfully completed <see cref="Task{TResult}"/>.</typeparam>
//        /// <param name="taskCompletionSource">The <see cref="TaskCompletionSource{TResult}"/> to cancel.</param>
//        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
//        /// <param name="cancellationCallback">A callback to invoke when cancellation occurs.</param>
//        public static void AttachCancellation<T>(this TaskCompletionSource<T> taskCompletionSource, CancellationToken cancellationToken, ICancellationNotification? cancellationCallback = null)
//        {
//            if (cancellationToken.CanBeCanceled && !taskCompletionSource.Task.IsCompleted)
//            {
//                if (cancellationToken.IsCancellationRequested)
//                {
//                    taskCompletionSource.TrySetCanceled(cancellationToken);
//                }
//                else
//                {
//                    var tuple = new CancelableTaskCompletionSource<T>(taskCompletionSource, cancellationCallback, cancellationToken);
//                    tuple.CancellationTokenRegistration = cancellationToken.UnsafeRegister(
//                        s =>
//                        {
//                            var t = (CancelableTaskCompletionSource<T>)s!;
//                            if (t.TaskCompletionSource.TrySetCanceled(t.CancellationToken))
//                            {
//                                t.CancellationCallback?.OnCanceled();
//                            }
//                        },
//                        tuple);

//                    // In certain race conditions, our continuation could execute inline. We could force it to always run
//                    // asynchronously, but then in the common case it becomes less efficient.
//                    // Instead, we will optimize for the common (no-race) case and detect if we were inlined, and if so, defer the work
//                    // to avoid making our caller block for arbitrary code since CTR.Dispose blocks for in-progress cancellation notification to complete.
//#pragma warning disable VSTHRD110 // Observe result of async calls
//                    taskCompletionSource.Task.ContinueWith(
//#pragma warning restore VSTHRD110 // Observe result of async calls
//                        (_, s) =>
//                        {
//                            var t = (CancelableTaskCompletionSource<T>)s!;
//                            if (t.ContinuationScheduled || !t.OnOwnerThread)
//                            {
//                                // We're not executing inline... Go ahead and do the work.
//                                t.CancellationTokenRegistration.Dispose();
//                            }
//                            else if (!t.CancellationToken.IsCancellationRequested) // If the CT is canceled, the CTR is implicitly disposed.
//                            {
//                                // We hit the race where the task is already completed another way,
//                                // and our continuation is executing inline with our caller.
//                                // Dispose our CTR from the threadpool to avoid blocking on 3rd party code.
//                                ThreadPool.QueueUserWorkItem(
//                                    s2 =>
//                                    {
//                                        var t2 = (CancelableTaskCompletionSource<T>)s2!;
//                                        t2.CancellationTokenRegistration.Dispose();
//                                    },
//                                    s);
//                            }
//                        },
//                        tuple,
//                        CancellationToken.None,
//                        TaskContinuationOptions.ExecuteSynchronously,
//                        TaskScheduler.Default);
//                    tuple.ContinuationScheduled = true;
//                }
//            }
//        }

//        /// <summary>
//        /// Cancels a <see cref="TaskCompletionSource.Task"/> if a given <see cref="CancellationToken"/> is canceled.
//        /// </summary>
//        /// <param name="taskCompletionSource">The <see cref="TaskCompletionSource"/> to cancel.</param>
//        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
//        /// <param name="cancellationCallback">A callback to invoke when cancellation occurs.</param>
//        public static void AttachCancellation(this TaskCompletionSource taskCompletionSource, CancellationToken cancellationToken, ICancellationNotification? cancellationCallback = null)
//        {
//            if (cancellationToken.CanBeCanceled && !taskCompletionSource.Task.IsCompleted)
//            {
//                if (cancellationToken.IsCancellationRequested)
//                {
//                    taskCompletionSource.TrySetCanceled(cancellationToken);
//                }
//                else
//                {
//                    var tuple = new CancelableTaskCompletionSource(taskCompletionSource, cancellationCallback, cancellationToken);
//                    tuple.CancellationTokenRegistration = cancellationToken.UnsafeRegister(
//                        s =>
//                        {
//                            var t = (CancelableTaskCompletionSource)s!;
//                            if (t.TaskCompletionSource.TrySetCanceled(t.CancellationToken))
//                            {
//                                t.CancellationCallback?.OnCanceled();
//                            }
//                        },
//                        tuple);

//                    // In certain race conditions, our continuation could execute inline. We could force it to always run
//                    // asynchronously, but then in the common case it becomes less efficient.
//                    // Instead, we will optimize for the common (no-race) case and detect if we were inlined, and if so, defer the work
//                    // to avoid making our caller block for arbitrary code since CTR.Dispose blocks for in-progress cancellation notification to complete.
//#pragma warning disable VSTHRD110 // Observe result of async calls
//                    taskCompletionSource.Task.ContinueWith(
//#pragma warning restore VSTHRD110 // Observe result of async calls
//                        (_, s) =>
//                        {
//                            var t = (CancelableTaskCompletionSource)s!;
//                            if (t.ContinuationScheduled || !t.OnOwnerThread)
//                            {
//                                // We're not executing inline... Go ahead and do the work.
//                                t.CancellationTokenRegistration.Dispose();
//                            }
//                            else if (!t.CancellationToken.IsCancellationRequested) // If the CT is canceled, the CTR is implicitly disposed.
//                            {
//                                // We hit the race where the task is already completed another way,
//                                // and our continuation is executing inline with our caller.
//                                // Dispose our CTR from the threadpool to avoid blocking on 3rd party code.
//                                ThreadPool.QueueUserWorkItem(
//                                    s2 =>
//                                    {
//                                        var t2 = (CancelableTaskCompletionSource)s2!;
//                                        t2.CancellationTokenRegistration.Dispose();
//                                    },
//                                    s);
//                            }
//                        },
//                        tuple,
//                        CancellationToken.None,
//                        TaskContinuationOptions.ExecuteSynchronously,
//                        TaskScheduler.Default);
//                    tuple.ContinuationScheduled = true;
//                }
//            }
//        }
    }
}
