// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Linq;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Core;
using Fluxzy.Rules;
using Fluxzy.Rules.Actions;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Authentication
{
    public class UpstreamPinningRuleTests
    {
        [Fact]
        public async Task Action_sets_require_pinning_flag()
        {
            var authority = new Authority("intranet.corp.test", 80, false);
            var context = new ExchangeContext(authority, new VariableContext(), null,
                SetUserAgentActionMapping.Default);

            await new PinUpstreamConnectionAction().InternalAlter(
                context, null, null, FilterScope.RequestHeaderReceivedFromClient, null!);

            Assert.True(context.RequireUpstreamPinning);
        }

        [Fact]
        public void Default_pinning_rule_is_registered()
        {
            var setting = FluxzySetting.CreateDefault();

            var pinningRule = setting.FixedRules()
                                     .FirstOrDefault(r => r.Action is PinUpstreamConnectionAction);

            Assert.NotNull(pinningRule);
        }

        [Fact]
        public void Default_pinning_rule_is_omitted_when_disabled()
        {
            var setting = FluxzySetting.CreateDefault()
                                       .SetDisableAutomaticConnectionAuthPinning(true);

            Assert.DoesNotContain(setting.FixedRules(), r => r.Action is PinUpstreamConnectionAction);
        }
    }
}
