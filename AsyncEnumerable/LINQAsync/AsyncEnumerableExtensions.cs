using AsyncEnumerable.Collections;
using System;
using System.Collections.Generic;
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

        public static Task<T> Aggregate<T>(
            this IEnumerable<Task<T>> tasks,
            Func<T,T,T> aggregator,
            CancellationToken cancellationToken = default)
        {
            return tasks.Aggregate(default, aggregator, cancellationToken);
        }

        public static Task<TAccumulate> Aggregate<TSource, TAccumulate>(
            this IEnumerable<Task<TSource>> tasks,
            TAccumulate seed,
            Func<TAccumulate, TSource, TAccumulate> aggregator,
            CancellationToken cancellationToken = default)
        {
            return tasks.Aggregate(seed, aggregator, t => t, cancellationToken);
        }

        public static async Task<TResult> Aggregate<TSource, TAccumulate, TResult>(
            this IEnumerable<Task<TSource>> tasks,
            TAccumulate seed,
            Func<TAccumulate, TSource, TAccumulate> aggregator,
            Func<TAccumulate, TResult> resultSelector,
            CancellationToken cancellationToken = default)
        {
            TAccumulate returnVal = seed;
            await foreach (var value in tasks.ReturnWhenComplete())
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                returnVal = aggregator(returnVal, value);
            }
            return resultSelector(returnVal);
        }

        public static async IAsyncEnumerable<T> WhereAsync<T>(
            this IEnumerable<Task<T>> tasks,
            Func<T, bool> predicate,
            [EnumeratorCancellation]
            CancellationToken cancellationToken = default)
        {
            await foreach (var task in tasks.ReturnWhenComplete())
            {
                if (cancellationToken.IsCancellationRequested)
                    yield break;
                if (predicate(task))
                    yield return task;
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
                if (cancellationToken.IsCancellationRequested)
                    yield break;
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
                if (cancellationToken.IsCancellationRequested)
                    yield break;
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
                if (cancellationToken.IsCancellationRequested)
                    yield break;
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
            await foreach (var value in tasks.ReturnWhenComplete())
            {
                if (cancellationToken.IsCancellationRequested)
                    yield break;
                if (predicate(value))
                    yield return value;
                else
                    yield break;
            }
        }

        public static async IAsyncEnumerable<TResult> SelectAsync<TSource, TResult>(
            this IEnumerable<Task<TSource>> tasks,
            Func<TSource, TResult> selector,
            [EnumeratorCancellation]
            CancellationToken cancellationToken = default)
        {
            await foreach(var value in tasks.ReturnWhenComplete())
            {
                if (cancellationToken.IsCancellationRequested)
                    yield break;
                yield return selector(value);
            }
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
