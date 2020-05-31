using AsyncEnumerable.Collections.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncEnumerable.Collections
{
    public class AsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        private IEnumerable<Task<AsyncEnumerableTaskResult<T>>> _tasks { get; set; }

        private AsyncEnumerable() { }

        public AsyncEnumerable(IEnumerable<Task<T>> tasks)
        {
            _tasks = tasks.Select(task => task.ContinueWith(async t =>
            {
                return new AsyncEnumerableTaskResult<T>
                {
                    Emit = true,
                    Result = await t,
                    Stop = false
                };
            },
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default).Unwrap());
        }

        internal static AsyncEnumerable<T> WithPredicate(AsyncEnumerable<T> source, Func<AsyncEnumerableTaskResult<T>, bool> predicate)
        {
            return new AsyncEnumerable<T>
            {
                _tasks = source._tasks.Select(task => task.ContinueWith(async t =>
                {
                    var result = await t;
                    if (!result.Emit)
                        return result;
                    return new AsyncEnumerableTaskResult<T>
                    {
                        Emit = result.Emit && predicate(result),
                        Result = result.Result,
                        Stop = result.Stop
                    };
                },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default).Unwrap())
            };
        }

        internal static AsyncEnumerable<T> WithStopCondition(AsyncEnumerable<T> source, Func<AsyncEnumerableTaskResult<T>, bool> stopCondition)
        {
            return new AsyncEnumerable<T>
            {
                _tasks = source._tasks.Select(task => task.ContinueWith(async t =>
                {
                    var result = await t;
                    if (!result.Emit)
                        return result;
                    return new AsyncEnumerableTaskResult<T>
                    {
                        Emit = result.Emit,
                        Result = result.Result,
                        Stop = result.Stop || stopCondition(result)
                    };
                },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default).Unwrap())
            };
        }

        internal static AsyncEnumerable<TResult> WithTransform<TSource, TResult>(AsyncEnumerable<TSource> source, Func<TSource, TResult> transform)
        {
            return new AsyncEnumerable<TResult>
            {
                _tasks = source._tasks.Select(task => task.ContinueWith(async t =>
                {
                    var result = await t;
                    if (!result.Emit)
                        return new AsyncEnumerableTaskResult<TResult>
                        {
                            Emit = result.Emit,
                            Result = default,
                            Stop = result.Stop
                        };
                    return new AsyncEnumerableTaskResult<TResult>
                    {
                        Emit = result.Emit,
                        Result = transform(result.Result),
                        Stop = result.Stop
                    };
                },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default).Unwrap())
            };
        }

        internal static AsyncEnumerable<T> WithStopConditionAndPredicate(AsyncEnumerable<T> source, Func<AsyncEnumerableTaskResult<T>, bool> stopCondition, Func<AsyncEnumerableTaskResult<T>, bool> predicate)
        {
            var enumerable = WithStopCondition(source, stopCondition);
            return WithPredicate(enumerable, predicate);
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new AsyncEnumerator(_tasks, cancellationToken);
        }

        private class AsyncEnumerator : IAsyncEnumerator<T>
        {
            private readonly SemaphoreSlim _itemReady;
            private readonly ConcurrentQueue<AsyncEnumerableTaskResult<T>> _processedItems = new ConcurrentQueue<AsyncEnumerableTaskResult<T>>();

            private bool _isDisposed = false;
            private bool _isStopped = false;
            private readonly int _totalCount = 0;
            private int _processedCount = 0;

            public AsyncEnumerator(IEnumerable<Task<AsyncEnumerableTaskResult<T>>> tasks, CancellationToken cancellationToken)
            {
                _itemReady = new SemaphoreSlim(0);
                foreach (var task in tasks)
                {
                    _totalCount++;
                    task.ContinueWith(async t =>
                    {
                        _processedItems.Enqueue(await t);
                        _itemReady.Release();
                    },
                    cancellationToken,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
                }
            }
            public T Current { get; private set; }

            public ValueTask DisposeAsync()
            {
                _itemReady.Dispose();
                _isDisposed = true;
                return new ValueTask();
            }

            public ValueTask<bool> MoveNextAsync()
            {
                if (_processedCount == _totalCount || _isStopped || _isDisposed)
                    return new ValueTask<bool>(false);
                return MoveNextAsyncRecursive();
            }

            private async ValueTask<bool> MoveNextAsyncRecursive()
            {
                await _itemReady.WaitAsync();
                Interlocked.Increment(ref _processedCount);
                var success = _processedItems.TryDequeue(out AsyncEnumerableTaskResult<T> result);
                if (result.Stop)
                    _isStopped = result.Stop;
                if (!result.Emit)
                    return await MoveNextAsync();
                Current = result.Result;
                return success;
            }
        }
    }
}
