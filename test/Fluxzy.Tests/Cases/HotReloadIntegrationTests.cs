// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters;
using Fluxzy.Tests._Fixtures;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Fluxzy.Tests.Cases
{
    public class HotReloadIntegrationTests
    {
        [Fact]
        public async Task UpdateRules_DuringProxyExecution_NewRulesApplyToSubsequentRequests()
        {
            // Arrange - Start proxy with initial rule
            var setting = FluxzySetting.CreateLocalRandomPort();
            setting.AddAlterationRules(
                new AddResponseHeaderAction("X-Initial-Rule", "initial"),
                AnyFilter.Default
            );

            await using var proxy = new Proxy(setting);
            using var client = HttpClientUtility.CreateHttpClient(proxy.Run(), setting);

            // Act - Make first request with initial rule
            var response1 = await client.GetAsync(TestConstants.GetHost("http2") + "/");
            var hasInitialHeader = response1.Headers.TryGetValues("X-Initial-Rule", out var initialValues);

            // Assert - Initial rule applied
            Assert.True(hasInitialHeader);
            Assert.Equal("initial", initialValues?.First());

            // Act - Update rules while proxy is running
            proxy.UpdateRules(s => {
                s.AddAlterationRules(
                    new AddResponseHeaderAction("X-Updated-Rule", "updated"),
                    AnyFilter.Default
                );
            });

            // Act - Make second request with updated rule
            var response2 = await client.GetAsync(TestConstants.GetHost("http2") + "/");
            var hasUpdatedHeader = response2.Headers.TryGetValues("X-Updated-Rule", out var updatedValues);
            var hasOldHeader = response2.Headers.TryGetValues("X-Initial-Rule", out _);

            // Assert - Updated rule applied, old rule removed
            Assert.True(hasUpdatedHeader);
            Assert.Equal("updated", updatedValues?.First());
            Assert.False(hasOldHeader);
        }

        [Fact]
        public async Task UpdateRules_MultipleUpdates_LastUpdateApplies()
        {
            // Arrange
            var setting = FluxzySetting.CreateLocalRandomPort();
            await using var proxy = new Proxy(setting);
            using var client = HttpClientUtility.CreateHttpClient(proxy.Run(), setting);

            // Act - Multiple rapid updates
            proxy.UpdateRules(s => s.AddAlterationRules(
                new AddResponseHeaderAction("X-Version", "1"),
                AnyFilter.Default
            ));

            proxy.UpdateRules(s => s.AddAlterationRules(
                new AddResponseHeaderAction("X-Version", "2"),
                AnyFilter.Default
            ));

            proxy.UpdateRules(s => s.AddAlterationRules(
                new AddResponseHeaderAction("X-Version", "3"),
                AnyFilter.Default
            ));

            // Make request after all updates
            var response = await client.GetAsync(TestConstants.GetHost("http2") + "/");
            var hasHeader = response.Headers.TryGetValues("X-Version", out var values);

            // Assert - Last update (version 3) is active
            Assert.True(hasHeader);
            Assert.Equal("3", values?.First());
        }
    }
}
