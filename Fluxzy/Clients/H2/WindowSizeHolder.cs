using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Clients.H2
{
    internal class WindowSizeHolder : IDisposable
    {
        private readonly H2Logger _logger;

        // private SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        private readonly Queue<TaskCompletionSource<object>> _windowSizeAWaiters = new();
        private volatile int _windowSize;

        public int WindowSize
        {
            get => _windowSize;

            private set => _windowSize = value;
        }

        public int StreamIdentifier { get; }

        public WindowSizeHolder(
            H2Logger logger,
            int windowSize,
            int streamIdentifier)
        {
            _logger = logger;
            WindowSize = windowSize;
            StreamIdentifier = streamIdentifier;
        }

        public void Dispose()
        {
            //_semaphore?.Dispose();
            // _semaphore = null;
        }

        public void UpdateWindowSize(int windowSizeIncrement)
        {
            _logger.Trace(this, windowSizeIncrement);

            lock (this)
            {
                if (WindowSize + (long)windowSizeIncrement > int.MaxValue)
                    WindowSize = int.MaxValue;
                else
                    WindowSize += windowSizeIncrement;
            }

            // This is not behaving as expected
            //_semaphore?.Release(_semaphore.CurrentCount);

            lock (_windowSizeAWaiters)
            {
                var list = new List<TaskCompletionSource<object?>>();

                while (_windowSizeAWaiters.TryDequeue(out var item))
                    list.Add(item);

                foreach (var item in list)
                {
                    item.SetResult(null);
                    ;
                }
            }
        }

        public async ValueTask<int> BookWindowSize(int requestedLength, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested || requestedLength == 0)
                return 0;

            lock (this)
            {
                var maxAvailable = Math.Min(requestedLength, WindowSize);

                if (maxAvailable > 0)
                {
                    WindowSize -= maxAvailable;

                    _logger.Trace(this, -maxAvailable);

                    return maxAvailable;
                }
            }

            var onJobReady = new TaskCompletionSource<object>();

            // sleep until window updated 

            lock (_windowSizeAWaiters)
            {
                _windowSizeAWaiters.Enqueue(onJobReady);
            }

            await onJobReady.Task;

            return await BookWindowSize(requestedLength, cancellationToken).ConfigureAwait(false);
        }
    }
}
