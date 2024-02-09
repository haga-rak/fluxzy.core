namespace Fluxzy.Rules.Actions
{
    internal class AverageThrottler
    {
        private readonly IInstantProvider _instantProvider;
        private readonly long _instantStart;
        private readonly long _bandwidthBytesPerMillis;

        private long _totalReceived;

        public AverageThrottler(long layer7BandwidthBytesPerSeconds, IInstantProvider instantProvider)
        {
            _instantProvider = instantProvider;
            _bandwidthBytesPerMillis = layer7BandwidthBytesPerSeconds / 1000L;
            _instantStart = instantProvider.ElapsedMillis;
        }

        public int ComputeThrottleDelay(int currentReadSize)
        {
            var instantReceive = _instantProvider.ElapsedMillis;
            _totalReceived += currentReadSize;

            var provisionalDelayMilliseconds = _totalReceived / (double)_bandwidthBytesPerMillis;

            var delay = (int)(provisionalDelayMilliseconds - (instantReceive - _instantStart));

            if (delay < 0)
            {
                delay = 0;
            }

            return delay;
        }
    }
}