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
        [Fact]
        public async Task TestFunctionality()
        {
            var maxMilliSeconds = 15 * 1000;
            var random = new Random();
            var watch = Stopwatch.StartNew();
            var tasks = Enumerable.Range(1, 1000)
                .Select(r =>
                {
                    var count = random.Next(0, maxMilliSeconds);
                    return Task.Delay(count).ContinueWith(_ => count);
                })
                .ToArray();
            int processed = 0;
            await foreach (var result in tasks.ReturnWhenComplete())
            {
                processed++;
            }
            watch.Stop();
            Assert.Equal(1000, processed);
            Assert.True(watch.ElapsedMilliseconds <= maxMilliSeconds + 100);
        }
    }
}
