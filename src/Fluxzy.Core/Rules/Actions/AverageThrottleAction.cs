// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;

namespace Fluxzy.Rules.Actions
{
    /// <summary>
    /// Throttle and simulate bandwidth condition.
    /// </summary>
    [ActionMetadata(
        "Throttle and simulate bandwidth condition.")]
    public class AverageThrottleAction : Action
    {
        private AverageThrottler? _averageThrottler;

        public override FilterScope ActionScope => FilterScope.RequestHeaderReceivedFromClient;

        public override string DefaultDescription => "Throttle";

        [ActionDistinctive(Description = "Bandwidth in bytes per seconds. Default is 64KB/s")]
        public long BandwidthBytesPerSeconds { get; set; } = 1024 * 64;

        [ActionDistinctive(Description = "Channel to be throttled")]
        public ThrottleChannel ThrottleChannel { get; set; } = ThrottleChannel.All;

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            // Not thread safe !
            _averageThrottler ??= new AverageThrottler(
                new ThrottlePolicy(BandwidthBytesPerSeconds), new DefaultInstantProvider()
            ); 

            if (ThrottleChannel.HasFlag(ThrottleChannel.Request)) {
                context.RegisterRequestBodySubstitution(
                    new AverageThrottleSubstitution(_averageThrottler));
            }

            return default; 
        }
    }

    [Flags]
    public enum ThrottleChannel
    {
        None,
        Request = 1,
        Response = 2,
        All = Request | Response
    }

    internal class AverageThrottleSubstitution : IStreamSubstitution
    {
        private readonly AverageThrottler _averageThrottler;

        public AverageThrottleSubstitution(AverageThrottler averageThrottler)
        {
            _averageThrottler = averageThrottler;
        }

        public ValueTask<Stream> Substitute(Stream originalStream)
        {
            return new ValueTask<Stream>(new BufferedThrottleStream(originalStream, _averageThrottler));
        }
    }

    public class ThrottlePolicy
    {
        public ThrottlePolicy(long layer7BandwidthBytesPerSeconds)
        {
            Layer7BandwidthBytesPerSeconds = layer7BandwidthBytesPerSeconds;
        }
        
        /// <summary>
        /// 
        /// </summary>
        public long Layer7BandwidthBytesPerSeconds { get; }
    }


    internal class AverageThrottler
    {
        private readonly IInstantProvider _instantProvider;
        private readonly long _instantStart;
        private readonly long _bandwidthBytesPerMillis;

        private long _totalReceived;

        public AverageThrottler(ThrottlePolicy proThrottlePolicy, IInstantProvider instantProvider)
        {
            _instantProvider = instantProvider; 
            _bandwidthBytesPerMillis = (proThrottlePolicy.Layer7BandwidthBytesPerSeconds / 1000L);
            _instantStart =  instantProvider.ElapsedMillis;
        }

        public int ComputeThrottleDelay(int currentReadSize)
        {
            var instantReceive = _instantProvider.ElapsedMillis;
            _totalReceived += currentReadSize;

            var provisionalDelayMilliseconds = (_totalReceived / (double)_bandwidthBytesPerMillis);

            var delay = (int) (provisionalDelayMilliseconds - (instantReceive - _instantStart));

            if (delay < 0) {
                delay = 0;
            }

            return delay;
        }
    }

    internal interface IInstantProvider
    {
        long ElapsedMillis { get;  }
    }


    internal class DefaultInstantProvider : IInstantProvider
    {
        public long ElapsedMillis => Stopwatch.GetTimestamp() / (Stopwatch.Frequency / 1000);
    }
}
