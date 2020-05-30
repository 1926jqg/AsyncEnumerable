using AsyncEnumerable.LINQAsync;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace AsyncEnumerable.Tests
{
    public class CollectionsTest
    {
        private const int millisecondMultiplier = 100;

        [Fact]
        public async Task TestFunctionality()
        {
            var tasksCount = 10000;
            var maxMilliSeconds = 15 * millisecondMultiplier;
            var random = new Random();
            var watch = Stopwatch.StartNew();
            var tasks = Enumerable.Range(1, tasksCount)
                .Select(r =>
                {
                    var count = random.Next(0, maxMilliSeconds);
                    return Task.Delay(count).ContinueWith(_ => count);
                })
                .ToArray();
            var processed = 0;
            var firstProcessedTime = -1L;
            await foreach (var result in tasks.ReturnWhenComplete())
            {
                if (firstProcessedTime < 0)
                    firstProcessedTime = watch.ElapsedMilliseconds;
                processed++;
            }
            watch.Stop();
            Assert.Equal(tasksCount, processed);
            Assert.True(watch.ElapsedMilliseconds <= maxMilliSeconds + 100);
            Assert.True(firstProcessedTime <= millisecondMultiplier);
        }
    }
}
