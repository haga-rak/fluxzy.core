// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Threading.Tasks;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters;
using Fluxzy.Rules.Filters.RequestFilters;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.Cases
{
    public class BadRuleHandlingTests
    {
        [Fact]
        public async Task HandleBadRuleException()
        {
            var setting = FluxzySetting.CreateLocalRandomPort();

            setting.ConfigureRule().When(
                       new HostFilter("*", StringSelectorOperation.Regex))
                   .Do(new AddResponseHeaderAction("yes", "no"));

            await using var proxy = new Proxy(setting);

            using var client = HttpClientUtility.CreateHttpClient(proxy.Run(), setting);
            
            var response = await client.GetAsync(TestConstants.Http2Host);

            var statusCode = response.StatusCode;
            var responseBodyString = await response.Content.ReadAsStringAsync();

            Assert.Equal(528, (int)statusCode);
        }
    }
}
