using System;
using System.Threading;
using System.Threading.Tasks;

namespace Echoes.H2.Cli
{
    public class WindowSizeHolder : IDisposable
    {
        private int _windowSize;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        private readonly SemaphoreSlim _semaphoreRequest = new SemaphoreSlim(1);
        private SpinLock _spinLock = new SpinLock();

        public WindowSizeHolder(int windowSize)
        {
            _windowSize = windowSize;
        }

        public int WindowSize => _windowSize;

        public void UpdateWindowSize(int value)
        {
            Interlocked.Add(ref _windowSize, value);
            _semaphore.Release(_semaphore.CurrentCount); 

            // Wakeup at least 
        }

        public async ValueTask<bool> BookWindowSize(int requestedLength, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return false;

            if (_windowSize >= requestedLength)
            try
            {
                await _semaphoreRequest.WaitAsync(cancellationToken).ConfigureAwait(false);

                if (_windowSize >= requestedLength)
                {
                    _windowSize -= requestedLength;
                    return true;
                }
            }
            finally
            {
                _semaphoreRequest.Release();
            }

            try
            {

                await _semaphore.WaitAsync(1000, cancellationToken).ConfigureAwait(false);
                return await BookWindowSize(requestedLength, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Dispose()
        {
            _semaphore?.Dispose();
            _semaphoreRequest?.Dispose();
            
        }
    }
}