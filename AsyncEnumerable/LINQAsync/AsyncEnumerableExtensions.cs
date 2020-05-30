using AsyncEnumerable.Collections;
using System;
using System.Collections.Generic;
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

        public static async Task<List<T>> ToListAsync<T>(
            this IAsyncEnumerable<T> source,
            CancellationToken cancellationToken = default)
        {
            var returnVal = new List<T>();
            await foreach (var value in source)
            {
                returnVal.Add(value);
            }
            return returnVal;
        }

        public static Task<T> AggregateAsync<T>(
            this IAsyncEnumerable<T> source,
            Func<T, T, T> aggregator,
            CancellationToken cancellationToken = default)
        {
            return source.AggregateAsync(default, aggregator, cancellationToken);
        }

        public static Task<TAccumulate> AggregateAsync<TSource, TAccumulate>(
            this IAsyncEnumerable<TSource> source,
            TAccumulate seed,
            Func<TAccumulate, TSource, TAccumulate> aggregator,
            CancellationToken cancellationToken = default)
        {
            return source.AggregateAsync(seed, aggregator, t => t, cancellationToken);
        }

        public static async Task<TResult> AggregateAsync<TSource, TAccumulate, TResult>(
            this IAsyncEnumerable<TSource> source,
            TAccumulate seed,
            Func<TAccumulate, TSource, TAccumulate> aggregator,
            Func<TAccumulate, TResult> resultSelector,
            CancellationToken cancellationToken = default)
        {
            TAccumulate returnVal = seed;
            await foreach (var value in source)
            {
                returnVal = aggregator(returnVal, value);
            }
            return resultSelector(returnVal);
        }

        public static IAsyncEnumerable<T> WhereAsync<T>(
            this IAsyncEnumerable<T> source,
            Func<T, bool> predicate,
            CancellationToken cancellationToken = default)
        {
            if (source is AsyncEnumerable<T> concreteSource)
            {
                return AsyncEnumerable<T>.WithPredicate(concreteSource, x => predicate(x.Result));
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public static IAsyncEnumerable<T> TakeAsync<T>(
            this IAsyncEnumerable<T> source,
            int count,
            CancellationToken cancellationToken = default)
        {
            if (count <= 0)
                return new AsyncEnumerable<T>(new List<Task<T>>());

            if (source is AsyncEnumerable<T> concreteSource)
            {
                var current = 0;
                return AsyncEnumerable<T>.WithStopCondition(concreteSource, t =>
                {
                    if (t.Emit)
                        ++current;
                    return current >= count;
                });
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public static IAsyncEnumerable<T> SkipAsync<T>(
            this IAsyncEnumerable<T> source,
            int count,
            CancellationToken cancellationToken = default)
        {
            var current = 0;
            if (source is AsyncEnumerable<T> concreteSource)
            {
                return AsyncEnumerable<T>.WithPredicate(concreteSource, t =>
                {
                    return current++ >= count;
                });
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        public static IAsyncEnumerable<T> SkipWhileAsync<T>(
            this IAsyncEnumerable<T> source,
            Func<T, bool> predicate,
            CancellationToken cancellationToken = default)
        {

            if (source is AsyncEnumerable<T> concreteSource)
            {
                var skip = true;
                return AsyncEnumerable<T>.WithPredicate(concreteSource, t =>
                {
                    if (skip && !predicate(t.Result))
                        skip = false;
                    if (skip)
                        return false;
                    else
                        return true;
                });
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public static IAsyncEnumerable<T> TakeWhileAsync<T>(
            this IAsyncEnumerable<T> source,
            Func<T, bool> predicate,
            CancellationToken cancellationToken = default)
        {
            if (source is AsyncEnumerable<T> concreteSource)
            {
                return AsyncEnumerable<T>.WithStopConditionAndPredicate(concreteSource, t =>
                {
                    return t.Emit && !predicate(t.Result);
                }, t => predicate(t.Result));
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public static IAsyncEnumerable<TResult> SelectAsync<TSource, TResult>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, TResult> selector,
            CancellationToken cancellationToken = default)
        {
            if (source is AsyncEnumerable<TSource> concreteSource)
            {
                return AsyncEnumerable<TSource>.WithTransform(concreteSource, selector);
            }
            else
            {
                throw new NotImplementedException();
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

        public static async Task<T> FirstOrDefaultAsync<T>(this IAsyncEnumerable<T> tasks, Func<T, bool> predicate, CancellationToken cancellationToken = default)
        {
            var enumerator = tasks.WhereAsync(predicate, cancellationToken).GetAsyncEnumerator(cancellationToken);
            if (await enumerator.MoveNextAsync())
                return enumerator.Current;
            else
                return default;
        }
    }
}
