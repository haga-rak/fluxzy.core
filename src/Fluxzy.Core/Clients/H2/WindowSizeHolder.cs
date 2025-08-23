// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

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
        private volatile int _availableWindowSize;

        public WindowSizeHolder(
            H2Logger logger,
            int availableWindowSize,
            int streamIdentifier)
        {
            _logger = logger;
            AvailableWindowSize = availableWindowSize;
            StreamIdentifier = streamIdentifier;
        }

        public int AvailableWindowSize {
            get => _availableWindowSize;

            private set => _availableWindowSize = value;
        }

        public int StreamIdentifier { get; }

        public void Dispose()
        {
            //_semaphore?.Dispose();
            // _semaphore = null;
        }

        public void UpdateWindowSize(int windowSizeIncrement)
        {
            _logger.Trace(this, windowSizeIncrement);

            lock (this) {
                if (_availableWindowSize + (long) windowSizeIncrement > int.MaxValue) {
                    _availableWindowSize = int.MaxValue;
                }
                else {
                    Interlocked.Add(ref _availableWindowSize, windowSizeIncrement);
                }
            }

            // This is not behaving as expected
            //_semaphore?.Release(_semaphore.CurrentCount);

            lock (_windowSizeAWaiters) {
                var list = new List<TaskCompletionSource<object?>>();

                while (_windowSizeAWaiters.TryDequeue(out var item)) {
                    list.Add(item!);
                }

                foreach (var item in list) {
                    item.SetResult(null);
                }
            }
        }

        public async ValueTask<int> BookWindowSize(int requestedLength, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested || requestedLength == 0) {
                return 0;
            }

            lock (this) {
                var maxAvailable = Math.Min(requestedLength, AvailableWindowSize);

                if (maxAvailable > 0) {
                    Interlocked.Add(ref _availableWindowSize, -maxAvailable);

                    _logger.Trace(this, -maxAvailable);

                    return maxAvailable;
                }
            }

            var onJobReady = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            // sleep until window updated 

            lock (_windowSizeAWaiters) {
                _windowSizeAWaiters.Enqueue(onJobReady);
            }

            await onJobReady.Task.ConfigureAwait(false);

            return await BookWindowSize(requestedLength, cancellationToken).ConfigureAwait(false);
        }
    }
}
