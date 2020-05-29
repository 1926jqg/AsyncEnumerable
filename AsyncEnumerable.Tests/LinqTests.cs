using AsyncEnumerable.LINQAsync;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace AsyncEnumerable.Tests
{
    public class LinqTests
    {

        private const int millisecondMultiplier = 100;

        private Task<int>[] GetTasks()
        {
            return new[] {
                Task.Delay(3 * millisecondMultiplier).ContinueWith(_ => 3),
                Task.Delay(1 * millisecondMultiplier).ContinueWith(_ => 1),
                Task.Delay(2 * millisecondMultiplier).ContinueWith(_ => 2),
                Task.Delay(5 * millisecondMultiplier).ContinueWith(_ => 5),
                Task.Delay(4 * millisecondMultiplier).ContinueWith(_ => 4),
            };
        }

        [Fact]
        public async Task TestSelectAsync()
        {
            int maxMilliseconds = 5 * millisecondMultiplier;

            var watch = Stopwatch.StartNew();
            var tasks = GetTasks();

            var processed = 0;
            await foreach(var result in tasks.SelectAsync(t => t * 2))
            {
                Assert.True(result % 2 == 0);
                processed++;
            }
            watch.Stop();
            Assert.Equal(5, processed);
            Assert.True(watch.ElapsedMilliseconds <= maxMilliseconds + 100);
        }

        [Fact]
        public async Task TestWhereAsync()
        {
            int maxMilliseconds = 5 * millisecondMultiplier;

            var watch = Stopwatch.StartNew();
            var tasks = GetTasks();

            var processed = 0;
            var lastTrue = 0L;
            await foreach (var result in tasks.WhereAsync(t => t <= 3))
            {
                Assert.True(result <= 3);
                processed++;
                lastTrue = watch.ElapsedMilliseconds;
            }
            watch.Stop();
            Assert.Equal(3, processed);
            Assert.True(watch.ElapsedMilliseconds <= maxMilliseconds + 100);
            Assert.True(lastTrue <= 3 * millisecondMultiplier + 100);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        public async Task TestTakeAsync(int take)
        {
            int maxMilliseconds = take * millisecondMultiplier;

            var watch = Stopwatch.StartNew();
            var tasks = GetTasks();

            var processed = 0;
            await foreach (var result in tasks.TakeAsync(take))
            {
                Assert.True(result <= take);
                processed++;
            }
            watch.Stop();
            Assert.Equal(take < 5 ? take : 5, processed);
            Assert.True(watch.ElapsedMilliseconds <= maxMilliseconds + 100);
        }

        [Fact]
        public async Task TestFirstOrDefault()
        {
            int maxMilliseconds = 1 * millisecondMultiplier;

            var watch = Stopwatch.StartNew();
            var tasks = GetTasks();

            var result = await tasks.FirstOrDefaultAsync();
            watch.Stop();
            Assert.Equal(1, result);
            Assert.True(watch.ElapsedMilliseconds <= maxMilliseconds + 100);
        }

        [Fact]
        public async Task TestFirstOrDefaultEmpty()
        {
            var tasks = new Task<int>[0];

            var result = await tasks.FirstOrDefaultAsync();
            Assert.Equal(default, result);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        public async Task TestFirstOrDefaultPredicate(int comparison)
        {
            int maxMilliseconds;
            int expected;
            if (comparison >= 1 && comparison <= 5)
            {
                maxMilliseconds = comparison * millisecondMultiplier;
                expected = comparison;
            }
            else
            {
                maxMilliseconds = 5 * millisecondMultiplier;
                expected = default;
            }
            var watch = Stopwatch.StartNew();
            var tasks = GetTasks();

            var result = await tasks.FirstOrDefaultAsync(t => t == comparison);
            watch.Stop();
                        
            Assert.True(watch.ElapsedMilliseconds <= maxMilliseconds + 100);
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task TestTakeWhileAsync()
        {
            int maxMilliseconds = 4 * millisecondMultiplier;

            var watch = Stopwatch.StartNew();
            var tasks = GetTasks();

            var processed = 0;
            var lastTrue = 0L;
            await foreach (var result in tasks.TakeWhileAsync(t => t <= 3))
            {
                Assert.True(result <= 3);
                processed++;
                lastTrue = watch.ElapsedMilliseconds;
            }
            watch.Stop();
            Assert.Equal(3, processed);
            Assert.True(watch.ElapsedMilliseconds <= maxMilliseconds + 100);
            Assert.True(lastTrue <= 3 * millisecondMultiplier + 100);
        }
    }
}
