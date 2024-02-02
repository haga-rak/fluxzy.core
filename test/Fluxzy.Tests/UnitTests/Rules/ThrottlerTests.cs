// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using Fluxzy.Rules.Actions;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Rules
{
    public class ThrottlerTests
    {
        [Fact]
        public void Do()
        {
            var instantProvider = new MockedInstantProvider();

            var throttlePolicy = new ThrottlePolicy(
                TimeSpan.FromSeconds(1), 1024); 

            var throttler = new Throttler(throttlePolicy, instantProvider);

            var delays = new List<int>();

            for (var i = 0; i < 10; i++) {
                var delay = throttler.ComputeThrottleDelay(1024);
                delays.Add(delay);
            }
            
        }
    }

    internal class MockedInstantProvider : IInstantProvider
    {
        private long _counter; 

        public MockedInstantProvider()
        {
        }

        public long ElapsedMillis {
            get
            {
                return (_counter++ * 1000);
            }
        }
    }
}
