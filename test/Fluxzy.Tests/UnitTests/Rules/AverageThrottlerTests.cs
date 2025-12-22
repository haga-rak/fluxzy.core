// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
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
            
            var throttler = new AverageThrottler(1024, instantProvider);

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

        [Theory]
        [InlineData(3)]
        [InlineData(2)]
        public async Task SimpleThrottle(int delaySeconds)
        {
            var setting = FluxzySetting.CreateLocalRandomPort();

            setting
                .ConfigureRule()
                .WhenAny()
                .Do(new AverageThrottleAction() {
                    BandwidthBytesPerSeconds = 1024 * 16
                }); 
            
            await using var proxy = new Proxy(setting);

            var endPoints = proxy.Run();

            var client = HttpClientUtility.CreateHttpClient(endPoints, setting);

            var bodySize = 1024 * 16 * delaySeconds + 1024;

            var url = $"https://sandbox.fluxzy.io/content-produce/{bodySize}/{bodySize}";

            var watch = Stopwatch.StartNew();

            var response = await client.GetAsync(url);
            var stream = await response.Content.ReadAsStreamAsync();
            await stream.CopyToAsync(Stream.Null);

            await Task.Delay(500);

            watch.Stop();

            Assert.True(watch.ElapsedMilliseconds > delaySeconds * 1000, 
                $"{watch.ElapsedMilliseconds} > {delaySeconds * 1000}");
        }
    }
}
