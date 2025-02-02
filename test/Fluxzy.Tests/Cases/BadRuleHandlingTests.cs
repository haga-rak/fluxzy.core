// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Linq;
using System.Threading.Tasks;
using Fluxzy.Rules;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Extensions;
using Fluxzy.Rules.Filters;
using Fluxzy.Rules.Filters.RequestFilters;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.Cases
{
    public class BadRuleHandlingTests
    {
        [Fact]
        public async Task HandleBadFilter1Exception()
        {
            await HandleGeneric(c => 
                c.When(new HostFilter("*", StringSelectorOperation.Regex))
                 .Do(new AddResponseHeaderAction("yes", "no")));
        }

        [Fact]
        public async Task HandleBadFilter2Exception()
        {
            await HandleGeneric(c => 
                c.When(new AbsoluteUriFilter("*", StringSelectorOperation.Regex))
                 .Do(new AddResponseHeaderAction("yes", "no")));
        }

        [Fact]
        public async Task HandleBadActionException()
        {
            await HandleGeneric(c => c.WhenAny().Do(new DelayAction(-10)));
        }

        private async Task HandleGeneric(Action<IConfigureFilterBuilder> builder)
        {
            var setting = FluxzySetting.CreateLocalRandomPort();

            builder(setting.ConfigureRule());

            await using var proxy = new Proxy(setting);

            using var client = HttpClientUtility.CreateHttpClient(proxy.Run(), setting);
            
            var response = await client.GetAsync(TestConstants.Http2Host);

            var content = await response.Content.ReadAsStringAsync();
            var hasHeader = response.Headers.TryGetValues("x-fluxzy-error-type", out var values);

            Assert.True(hasHeader);
            Assert.NotNull(values);
            Assert.Equal(nameof(RuleExecutionFailureException), values.First());
        }
    }
}
