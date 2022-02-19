using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Echoes.H2
{
    internal class WindowSizeHolder : IDisposable
    {
        private readonly H2Logger _logger;
        private int _windowSize;
        private readonly int _streamIdentifier;

        // private SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        private Queue<TaskCompletionSource<object>> _windowSizeAWaiters = new(); 

        public WindowSizeHolder(
            H2Logger logger, 
            int windowSize, 
            int streamIdentifier)
        {
            _logger = logger;
            _windowSize = windowSize;
            _streamIdentifier = streamIdentifier;
        }
        
        public int WindowSize => _windowSize;

        public int StreamIdentifier => _streamIdentifier;

        public void UpdateWindowSize(int windowSizeIncrement)
        {
            _logger.Trace(this, windowSizeIncrement);

            lock (this)
            {
                if ((_windowSize + ((long) windowSizeIncrement)) > int.MaxValue)
                {
                    _windowSize = int.MaxValue; 
                }
                else
                    _windowSize += windowSizeIncrement; 
            }

            // This is not behaving as expected
            //_semaphore?.Release(_semaphore.CurrentCount);

            lock (_windowSizeAWaiters)
            {
                var list = new List<TaskCompletionSource<object>>(); 
                while (_windowSizeAWaiters.TryDequeue(out var item))
                {
                    list.Add(item);
                }

                foreach (var item in list)
                {
                    item.SetResult(null);
                    ; 
                }
            }
        }

        private int _waitCount = 0; 

        public async ValueTask<int> BookWindowSize(int requestedLength, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested || requestedLength == 0)
                return 0;
            
            lock (this)
            {
                var maxAvailable = Math.Min(requestedLength, _windowSize);

                if (maxAvailable > 0)
                {
                    _windowSize -= maxAvailable;

                    _logger.Trace(this, -maxAvailable);

                    return maxAvailable;
                }
            }

            try
            {
                var onJobReady = new TaskCompletionSource<object>();

                // sleep until window updated 

                lock (_windowSizeAWaiters)
                    _windowSizeAWaiters.Enqueue(onJobReady);

                await onJobReady.Task; 
                return await BookWindowSize(requestedLength, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
               // _semaphore.Release();
            }
        }

        public void Dispose()
        {
            //_semaphore?.Dispose();
            // _semaphore = null;


        }
    }
}