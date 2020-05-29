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

        private Task<int>[] GetTasks()
        {
            return new[] {
                Task.Delay(3000).ContinueWith(_ => 3),
                Task.Delay(1000).ContinueWith(_ => 1),
                Task.Delay(2000).ContinueWith(_ => 2),
                Task.Delay(5000).ContinueWith(_ => 5),
                Task.Delay(4000).ContinueWith(_ => 4),
            };
        }

        [Fact]
        public async Task TestSelectAsync()
        {
            int maxMilliseconds = 5 * 1000;

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
            int maxMilliseconds = 5 * 1000;

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
            Assert.True(lastTrue <= 3 * 1000 + 100);
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
            int maxMilliseconds = take * 1000;

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
            int maxMilliseconds = 1 * 1000;

            var watch = Stopwatch.StartNew();
            var tasks = GetTasks();

            var result = await tasks.FirstOrDefaultAsync();
            watch.Stop();
            Assert.Equal(1, result);
            Assert.True(watch.ElapsedMilliseconds <= maxMilliseconds + 100);
        }

        [Theory]
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
                maxMilliseconds = comparison * 1000;
                expected = comparison;
            }
            else
            {
                maxMilliseconds = 5 * 1000;
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
            int maxMilliseconds = 4 * 1000;

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
            Assert.True(lastTrue <= 3 * 1000 + 100);
        }
    }
}
