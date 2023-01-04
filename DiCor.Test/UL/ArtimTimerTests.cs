using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DiCor.Net.UpperLayer;
using Xunit;

namespace DiCor.Test.UL
{
    public static class ArtimTimerTests
    {
        [Fact]
        public static async Task ManySynchronousStarts()
        {
            TimeSpan timeout = TimeSpan.FromSeconds(.1);
            int timeoutCount = 0;

            ULConnection.ArtimTimer<object?> timer = new(OnTimeout, null);

            for (int i = 0; i < 100; i++)
                timer.Start(timeout);

            await Task.Delay(timeout.Add(TimeSpan.FromSeconds(.1)));

            Assert.Equal(1, timeoutCount);

            void OnTimeout(object? state)
            {
                timeoutCount++;
            }
        }

        [Fact]
        public static async Task ManySynchronousStartsOneStop()
        {
            TimeSpan timeout = TimeSpan.FromSeconds(.1);
            int timeoutCount = 0;

            ULConnection.ArtimTimer<object?> timer = new(OnTimeout, null);

            for (int i = 0; i < 100; i++)
                timer.Start(timeout);

            timer.Complete();

            await Task.Delay(timeout.Add(TimeSpan.FromSeconds(.1)));

            Assert.Equal(0, timeoutCount);

            void OnTimeout(object? state)
            {
                timeoutCount++;
            }
        }

        [Fact]
        public static async Task ManyParallelStarts()
        {
            TimeSpan timeout = TimeSpan.FromSeconds(.1);
            int timeoutCount = 0;

            ULConnection.ArtimTimer<object?> timer = new(OnTimeout, null);

            Thread[] threads = Enumerable.Range(0, 100).Select(_ => new Thread(() => timer.Start(timeout))).ToArray();
            foreach (Thread thread in threads)
                thread.Start();
            foreach (Thread thread in threads)
                thread.Join();

            await Task.Delay(timeout.Add(TimeSpan.FromSeconds(.1)));

            Assert.Equal(1, timeoutCount);

            void OnTimeout(object? state)
            {
                timeoutCount++;
            }
        }

        [Fact]
        public static async Task ManyParallelStartsOneStop()
        {
            TimeSpan timeout = TimeSpan.FromSeconds(.1);
            int timeoutCount = 0;

            ULConnection.ArtimTimer<object?> timer = new(OnTimeout, null);

            Thread[] threads = Enumerable.Range(0, 100).Select(_ => new Thread(() => timer.Start(timeout))).ToArray();
            foreach (Thread thread in threads)
                thread.Start();
            foreach (Thread thread in threads)
                thread.Join();

            timer.Complete();

            await Task.Delay(timeout.Add(TimeSpan.FromSeconds(.1)));

            Assert.Equal(0, timeoutCount);

            void OnTimeout(object? state)
            {
                timeoutCount++;
            }
        }
    }
}
