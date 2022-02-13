using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Echoes.H2
{
    public class WindowSizeHolder : IDisposable
    {
        private long _windowSize;
        private readonly int _streamIdentifier;

        private SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        private SemaphoreSlim _semaphoreRequest = new SemaphoreSlim(1);

        public WindowSizeHolder(int windowSize, int streamIdentifier)
        {
            _windowSize = windowSize;
            _streamIdentifier = streamIdentifier;
        }

        public long WindowSize => _windowSize;

        public int StreamIdentifier => _streamIdentifier;

        public void UpdateWindowSize(int windowSizeIncrement)
        {
            Interlocked.Add(ref _windowSize, windowSizeIncrement);
            _semaphore?.Release(_semaphore.CurrentCount);



            if (StreamIdentifier == 0)
            {
                Debug.WriteLine("Window Size : "  + _windowSize);
            }
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
            _semaphore = null;
            _semaphoreRequest?.Dispose();
            _semaphoreRequest = null;


        }
    }
}