using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncEnumerable.Collections
{
    public class AsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        private readonly IEnumerable<Task<T>> _tasks;

        public AsyncEnumerable(IEnumerable<Task<T>> tasks)
        {
            _tasks = tasks;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new AsyncEnumerator<T>(_tasks, cancellationToken);
        }

        private class AsyncEnumerator<S> : IAsyncEnumerator<S>
        {
            private readonly SemaphoreSlim _itemReady;

            private readonly ConcurrentQueue<S> _processedItems = new ConcurrentQueue<S>();
            private int _processedCount = 0;
            private readonly int _totalCount = 0;

            public AsyncEnumerator(IEnumerable<Task<S>> tasks, CancellationToken cancellationToken)
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

            private S _current;
            public S Current
            {
                get
                {
                    return _current;
                }
            }

            public ValueTask DisposeAsync()
            {
                _itemReady.Dispose();
                return new ValueTask();
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (_processedCount == _totalCount)
                    return false;
                await _itemReady.WaitAsync();
                _processedCount++;
                return _processedItems.TryDequeue(out _current);
            }
        }
    }
}
