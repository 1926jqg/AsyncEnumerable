using AsyncEnumerable.LINQAsync;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace AsyncEnumerable.Tests
{
    public class LinqTests
    {
        private const int millisecondMultiplier = 100;
        private const int errorMargin = 50;

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
            await foreach (var result in tasks.ReturnWhenComplete().SelectAsync(t => t * 2))
            {
                Assert.True(result % 2 == 0);
                processed++;
            }
            watch.Stop();
            Assert.Equal(5, processed);
            Assert.True(watch.ElapsedMilliseconds <= maxMilliseconds + errorMargin);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        public async Task TestWhereAsync(int query)
        {
            int maxMilliseconds = 5 * millisecondMultiplier;

            var watch = Stopwatch.StartNew();
            var tasks = GetTasks();

            var processed = 0;
            var lastTrue = 0L;
            await foreach (var result in tasks.ReturnWhenComplete().WhereAsync(t => t <= query))
            {
                Assert.True(result <= query);
                processed++;
                lastTrue = watch.ElapsedMilliseconds;
            }
            watch.Stop();
            Assert.Equal(query, processed);
            Assert.True(watch.ElapsedMilliseconds <= maxMilliseconds + errorMargin);
            Assert.True(lastTrue <= query * millisecondMultiplier + errorMargin);
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
            await foreach (var result in tasks.ReturnWhenComplete().TakeAsync(take))
            {
                Assert.True(result <= take);
                processed++;
            }
            watch.Stop();
            Assert.Equal(take < 5 ? take : 5, processed);
            Assert.True(watch.ElapsedMilliseconds <= maxMilliseconds + errorMargin);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        public async Task TestSkipAsync(int skip)
        {
            int recordsToProcess = skip < 5 ? 5 - skip : 0;
            int maxMilliseconds = 5 * millisecondMultiplier;
            int firstProcessedTarget = (skip + 1) * millisecondMultiplier;

            var watch = Stopwatch.StartNew();
            var tasks = GetTasks();

            var processed = 0;
            var firstProcessed = -1L;
            await foreach (var result in tasks.ReturnWhenComplete().SkipAsync(skip))
            {
                if (firstProcessed < 0)
                    firstProcessed = watch.ElapsedMilliseconds;
                processed++;
            }
            watch.Stop();
            Assert.Equal(recordsToProcess, processed);
            Assert.True(watch.ElapsedMilliseconds <= maxMilliseconds + errorMargin);
            Assert.True(firstProcessed <= firstProcessedTarget + errorMargin);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        public async Task TestSkipWhileAsync(int skip)
        {
            int recordsToProcess = skip < 5 ? 5 - skip : 0;
            int maxMilliseconds = 5 * millisecondMultiplier;
            int firstProcessedTarget = (skip + 1) * millisecondMultiplier;

            var watch = Stopwatch.StartNew();
            var tasks = GetTasks();

            var processed = 0;
            var firstProcessed = -1L;
            await foreach (var result in tasks.ReturnWhenComplete().SkipWhileAsync(t => t <= skip))
            {
                if (firstProcessed < 0)
                    firstProcessed = watch.ElapsedMilliseconds;
                processed++;
            }
            watch.Stop();
            Assert.Equal(recordsToProcess, processed);
            Assert.True(watch.ElapsedMilliseconds <= maxMilliseconds + errorMargin);
            Assert.True(firstProcessed <= firstProcessedTarget + errorMargin);
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
            Assert.True(watch.ElapsedMilliseconds <= maxMilliseconds + errorMargin);
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

            var result = await tasks.ReturnWhenComplete().FirstOrDefaultAsync(t => t == comparison);
            watch.Stop();

            Assert.True(watch.ElapsedMilliseconds <= maxMilliseconds + errorMargin);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        public async Task TestTakeWhileAsync(int take)
        {
            int maxMilliseconds = (take + 1) * millisecondMultiplier;
            int recordsToProcess = take < 5 ? take : 5;

            var watch = Stopwatch.StartNew();
            var tasks = GetTasks();

            var processed = 0;
            var lastTrue = 0L;
            await foreach (var result in tasks.ReturnWhenComplete().TakeWhileAsync(t => t <= take))
            {
                Assert.True(result <= take);
                processed++;
                lastTrue = watch.ElapsedMilliseconds;
            }
            watch.Stop();
            Assert.Equal(recordsToProcess, processed);
            Assert.True(watch.ElapsedMilliseconds <= maxMilliseconds + errorMargin);
            Assert.True(lastTrue <= take * millisecondMultiplier + errorMargin);
        }

        [Fact]
        public async Task TestAggregate()
        {
            int maxMilliseconds = 5 * millisecondMultiplier;
            var watch = Stopwatch.StartNew();
            var result = await GetTasks()
                .ReturnWhenComplete()
                .AggregateAsync((x, y) => x + y);
            watch.Stop();
            Assert.Equal(15, result);
            Assert.True(watch.ElapsedMilliseconds <= maxMilliseconds + errorMargin);
        }

        [Fact]
        public async Task TestChainSkipTake()
        {
            var result = await GetTasks()
                .ReturnWhenComplete()
                .SkipAsync(1)
                .TakeAsync(2)
                .ToListAsync();
            Assert.DoesNotContain(1, result);
            Assert.Contains(2, result);
            Assert.Contains(3, result);
            Assert.DoesNotContain(4, result);
            Assert.DoesNotContain(5, result);
        }

        [Fact]
        public async Task TestChainTakeSkip()
        {
            var result = await GetTasks()
                .ReturnWhenComplete()
                .TakeAsync(3)
                .SkipAsync(1)
                .ToListAsync();
            Assert.DoesNotContain(1, result);
            Assert.Contains(2, result);
            Assert.Contains(3, result);
            Assert.DoesNotContain(4, result);
            Assert.DoesNotContain(5, result);
        }

        [Fact]
        public async Task TestChainTakeSkipTake()
        {
            var result = await GetTasks()
                .ReturnWhenComplete()
                .TakeAsync(4)
                .SkipAsync(1)
                .TakeAsync(2)
                .ToListAsync();
            Assert.DoesNotContain(1, result);
            Assert.Contains(2, result);
            Assert.Contains(3, result);
            Assert.DoesNotContain(4, result);
            Assert.DoesNotContain(5, result);
        }

        [Fact]
        public async Task TestChainSkipTakeWhile()
        {
            var result = await GetTasks()
                .ReturnWhenComplete()
                .SkipAsync(1)
                .TakeWhileAsync(t => t <= 3)
                .ToListAsync();
            Assert.DoesNotContain(1, result);
            Assert.Contains(2, result);
            Assert.Contains(3, result);
            Assert.DoesNotContain(4, result);
            Assert.DoesNotContain(5, result);
        }
    }
}
