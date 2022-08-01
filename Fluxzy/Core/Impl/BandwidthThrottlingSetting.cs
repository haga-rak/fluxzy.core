using System;

namespace Fluxzy.Core
{
    internal class BandwidthThrottlingSetting
    {
        public bool Enabled { get; set; }

        /// <summary>
        /// Limitation in Ko/s (Not Kb)
        /// </summary>
        public long BytePerSeconds { get; set; } 
        
        /// <summary>
        /// The time window where the bandwith throttle is applied
        /// </summary>
        public TimeSpan CheckInterval { get; set; }
    }
}