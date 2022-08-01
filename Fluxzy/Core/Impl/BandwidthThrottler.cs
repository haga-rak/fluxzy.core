using System;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Core.Utils;

namespace Fluxzy.Core
{
    /// <summary>
    /// The purpose of BandwidthLimiter is to create throttle 
    /// </summary>
    internal class BandwidthThrottler
    {
        private readonly BandwidthThrottlingSetting _limitationSetting;
        private readonly IReferenceClock _referenceClock;
        private DateTime _lastCaptureInstant;
        private long _capturedBytesInInterval = 0; 

        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1,1);

        public BandwidthThrottler(BandwidthThrottlingSetting limitationSetting, IReferenceClock referenceClock)
        {
            _limitationSetting = limitationSetting;
            _referenceClock = referenceClock;
        }

        public async Task CreateDelay(long currentBuffer)
        {
            if (!_limitationSetting.Enabled)
                return;

            _capturedBytesInInterval += currentBuffer;

            if (_lastCaptureInstant == DateTime.MinValue)
            {
                
                _lastCaptureInstant = ITimingProvider.Default.Instant();
                return; 
            }

            var now = ITimingProvider.Default.Instant();
            var elapsed = now - _lastCaptureInstant;
            
            if (elapsed < _limitationSetting.CheckInterval)
                return;  // The interval was not reached yet 

            var shouldBeMilliseconds = (int) (1000D * _capturedBytesInInterval / (double)_limitationSetting.BytePerSeconds);
            var actualElapsed = (int) elapsed.TotalMilliseconds;
            var waitInterval = shouldBeMilliseconds - actualElapsed;

            _lastCaptureInstant = ITimingProvider.Default.Instant();
            _capturedBytesInInterval = 0;

            if (waitInterval > 0)
            {
                using (await QuickSlim.Lock(_semaphoreSlim).ConfigureAwait(false))
                {
                    // this should never be in parrallel 
                    await Task.Delay(waitInterval).ConfigureAwait(false);
                }
            }

        }
    }
}