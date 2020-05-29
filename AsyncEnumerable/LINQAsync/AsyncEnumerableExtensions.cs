using AsyncEnumerable.Collections;
using AsyncEnumerable.LINQAsync.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncEnumerable.LINQAsync
{
    public static class AsyncEnumerableExtensions
    {
        public static IAsyncEnumerable<T> ReturnWhenComplete<T>(this IEnumerable<Task<T>> tasks)
        {
            return new AsyncEnumerable<T>(tasks);
        }

        public static async IAsyncEnumerable<T> WhereAsync<T>(
            this IEnumerable<Task<T>> tasks,
            Func<T, bool> predicate,
            [EnumeratorCancellation]
            CancellationToken cancellationToken = default)
        {
            var boolTasks = tasks
                .Select(t => t.ContinueWith(async t => new PredicateResult<T>
                {
                    Match = predicate(await t),
                    Value = await t
                },
                cancellationToken,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default).Unwrap());
            await foreach (var task in boolTasks.ReturnWhenComplete())
            {
                if (task.Match)
                    yield return task.Value;
            }
        }

        public static async IAsyncEnumerable<T> TakeAsync<T>(
            this IEnumerable<Task<T>> tasks,
            int count,
            [EnumeratorCancellation]
            CancellationToken cancellationToken = default)
        {
            if (count <= 0)
                yield break;

            var current = 0;
            await foreach (var task in tasks.ReturnWhenComplete())
            {

                yield return task;
                if (++current >= count)
                    yield break;
            }
        }

        public static async IAsyncEnumerable<T> SkipAsync<T>(
            this IEnumerable<Task<T>> tasks,
            int count,
            [EnumeratorCancellation]
            CancellationToken cancellationToken = default)
        {
            var current = 0;
            await foreach (var task in tasks.ReturnWhenComplete())
            {
                if (current++ < count)
                    continue;
                yield return task;                
            }
        }
        public static async IAsyncEnumerable<T> SkipWhileAsync<T>(
            this IEnumerable<Task<T>> tasks,
            Func<T, bool> predicate,
            [EnumeratorCancellation]
            CancellationToken cancellationToken = default)
        {
            var skip = true;

            await foreach (var task in tasks.ReturnWhenComplete())
            {
                if (skip && !predicate(task))
                    skip = false;
                if (!skip)
                    yield return task;
            }
        }

        public static async IAsyncEnumerable<T> TakeWhileAsync<T>(
            this IEnumerable<Task<T>> tasks,
            Func<T, bool> predicate,
            [EnumeratorCancellation]
            CancellationToken cancellationToken = default)
        {
            var boolTasks = tasks
                .Select(t => t.ContinueWith(async t => new PredicateResult<T>
                {
                    Match = predicate(await t),
                    Value = await t
                },
                cancellationToken,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default).Unwrap());
            await foreach (var task in boolTasks.ReturnWhenComplete())
            {
                if (task.Match)
                    yield return task.Value;
                else
                    yield break;
            }
        }

        public static IAsyncEnumerable<TResult> SelectAsync<TSource, TResult>(
            this IEnumerable<Task<TSource>> tasks,
            Func<TSource, TResult> selector,
            CancellationToken cancellationToken = default)
        {
            return tasks
                .Select(t => t.ContinueWith(async t => selector(await t),
                cancellationToken,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default).Unwrap())
                .ReturnWhenComplete();
        }

        public static async Task<T> FirstOrDefaultAsync<T>(this IEnumerable<Task<T>> tasks, CancellationToken cancellationToken = default)
        {
            var enumerator = tasks.ReturnWhenComplete().GetAsyncEnumerator(cancellationToken);
            if (await enumerator.MoveNextAsync())
                return enumerator.Current;
            else
                return default;
        }

        public static async Task<T> FirstOrDefaultAsync<T>(this IEnumerable<Task<T>> tasks, Func<T, bool> predicate, CancellationToken cancellationToken = default)
        {
            var enumerator = tasks.WhereAsync(predicate, cancellationToken).GetAsyncEnumerator(cancellationToken);
            if (await enumerator.MoveNextAsync())
                return enumerator.Current;
            else
                return default;
        }
    }
}
