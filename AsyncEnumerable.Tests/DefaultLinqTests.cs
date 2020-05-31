using AsyncEnumerable.LINQAsync;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace AsyncEnumerable.Tests
{
    public class DefaultLinqTests
    {
        private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(
            IEnumerable<T> source,
            int millisecondDelay = 10)
        {
            foreach(var item in source)
            {
                await Task.Delay(millisecondDelay);
                yield return item;
            }
        }

        [Fact]
        public async Task CompositeTest()
        {
            var enumerable = Enumerable.Range(1, 100);
            var asyncEnumerable = ToAsyncEnumerable(enumerable);


            var asyncList = await asyncEnumerable
                .SkipAsync(5)
                .SkipWhileAsync(i => i <= 10)
                .TakeAsync(95)
                .TakeAsync(85)
                .TakeWhileAsync(i => i <= 90)
                .WhereAsync(i => i % 2 == 0)
                .WhereAsync(async i =>
                {
                    await Task.Delay(1);
                    return i % 3 == 0;
                })
                .SelectAsync(i => i * 2)
                .ToListAsync();

            var list = enumerable
                .Skip(5)
                .SkipWhile(i => i <= 10)
                .Take(95)
                .Take(85)
                .TakeWhile(i => i <= 90)
                .Where(i => i % 2 == 0)
                .Where(i => i % 3 == 0)
                .Select(i => i * 2)
                .ToList();

            Assert.Equal(list.Count, asyncList.Count);
            foreach(var item in list)
            {
                Assert.Contains(item, asyncList);
            }
        }
    }
}
