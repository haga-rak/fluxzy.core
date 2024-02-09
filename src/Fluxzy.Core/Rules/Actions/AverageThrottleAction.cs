// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

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

        public override void Init(StartupContext startupContext)
        {
            base.Init(startupContext);

            _averageThrottler = new AverageThrottler(BandwidthBytesPerSeconds, 
                new DefaultInstantProvider()
            );
        }

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            if (ThrottleChannel.HasFlag(ThrottleChannel.Request)) {
                context.RegisterRequestBodySubstitution(
                    new AverageThrottleSubstitution(_averageThrottler!));
            }

            if (ThrottleChannel.HasFlag(ThrottleChannel.Response)) {
                context.RegisterResponseBodySubstitution(
                    new AverageThrottleSubstitution(_averageThrottler!));
            }

            return default; 
        }
    }
}
