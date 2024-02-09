// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using Fluxzy.Rules.Actions;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Rules
{
    public class AverageThrottlerTests
    {
        [Fact]
        public void Simple_Delay()
        {
            var instantProvider = new MockedInstantProvider();

            var throttlePolicy = new ThrottlePolicy(1024); 

            var throttler = new AverageThrottler(throttlePolicy, instantProvider);

            var delays = new List<int>();

            for (var i = 0; i < 10; i++) {
                var delay = throttler.ComputeThrottleDelay(i%2 == 0 ? 1024 + 512 : 1024 - 512);
                delays.Add(delay);
            }

            var diffs = new List<int>();

            for (var i = 0; i < delays.Count - 1; i++) {
                diffs.Add(delays[i + 1] - delays[i]);
            }
        }
    }
}
