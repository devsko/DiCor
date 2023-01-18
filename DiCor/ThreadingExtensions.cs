using System.Threading.Tasks;

namespace DiCor
{
    public static class ThreadingExtensions
    {
        public static void IgnoreExceptions(this ValueTask task)
        {
            IgnoreExceptionsAsync(task);

            static async void IgnoreExceptionsAsync(ValueTask task)
            {
                try { await task.ConfigureAwait(false); } catch { }
            }
        }

        /// <summary>Awaits a task, ignoring any resulting exceptions.</summary>
        public static void IgnoreExceptions(this Task task)
        {
            IgnoreExceptionsAsync(task);

            static async void IgnoreExceptionsAsync(Task task)
            {
                try { await task.ConfigureAwait(false); } catch { }
            }
        }
    }
}
