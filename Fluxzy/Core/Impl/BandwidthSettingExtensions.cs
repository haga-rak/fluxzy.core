using System;
using System.IO;

namespace Fluxzy.Core
{
    internal static class BandwidthSettingExtensions
    {
        internal static Stream GetThrottlerStream(this ProxyStartupSetting startUpSetting)
        {
            var referenceClock = new ReferenceClock();
            
            if (startUpSetting.ThrottleKBytePerSecond <= 0 || startUpSetting.ThrottleIntervalCheck < TimeSpan.FromMilliseconds(10))
            {
                return new NoThrottleStream();
            }

            var bandwidthThrottlerSetting = new BandwidthThrottlingSetting()
            {
                Enabled = true,
                BytePerSeconds = startUpSetting.ThrottleKBytePerSecond * 1024,
                CheckInterval = startUpSetting.ThrottleIntervalCheck
            };

            return new BandwidthThrottlerStream(new BandwidthThrottler(bandwidthThrottlerSetting, referenceClock));
        }
    }
}