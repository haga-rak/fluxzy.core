// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Linq;
using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Rules;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Rules
{
    public class UpStreamProxyActionTests
    {
        [Theory]
        // exact host
        [InlineData("example.com", "example.com", true)]
        [InlineData("Example.COM", "example.com", true)]
        [InlineData("other.com", "example.com", false)]
        // bare domain matches subdomains
        [InlineData("api.example.com", "example.com", true)]
        [InlineData("a.b.example.com", "example.com", true)]
        // no false suffix match
        [InlineData("notexample.com", "example.com", false)]
        [InlineData("evil-example.com", "example.com", false)]
        // explicit wildcard
        [InlineData("api.internal.lan", "*.internal.lan", true)]
        [InlineData("internal.lan", "*.internal.lan", true)]
        [InlineData("internal.lan", ".internal.lan", true)]
        [InlineData("somethingelse.lan", "*.internal.lan", false)]
        // catch all
        [InlineData("anything.at.all", "*", true)]
        // empty / blank entries are ignored
        [InlineData("example.com", "  ", false)]
        public void IsByPassed_Matches_Expected(string hostName, string entry, bool expected)
        {
            var result = UpStreamProxyAction.IsByPassed(new[] { entry }, hostName);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void IsByPassed_Empty_List_Never_Matches()
        {
            Assert.False(UpStreamProxyAction.IsByPassed(System.Array.Empty<string>(), "example.com"));
        }

        [Fact]
        public void IsByPassed_Null_List_Never_Matches()
        {
            Assert.False(UpStreamProxyAction.IsByPassed(null, "example.com"));
        }

        [Fact]
        public async Task InternalAlter_Sets_Proxy_When_ByPassHosts_Is_Null()
        {
            // ByPassHosts has a public setter and can be reset to null by a caller or by a
            // rule file carrying an empty byPassHosts scalar. This must not throw.
            var action = new UpStreamProxyAction("192.168.1.9", 8080) {
                ByPassHosts = null!
            };

            var context = CreateContext("www.example.com");

            await action.InternalAlter(context, null, null, FilterScope.OnAuthorityReceived, null!);

            Assert.NotNull(context.ProxyConfiguration);
            Assert.Equal("192.168.1.9", context.ProxyConfiguration!.Host);
        }

        [Fact]
        public async Task InternalAlter_Sets_Proxy_When_Host_Not_Bypassed()
        {
            var action = new UpStreamProxyAction("192.168.1.9", 8080) {
                ByPassHosts = { "localhost", "*.internal.lan" }
            };

            var context = CreateContext("www.example.com");

            await action.InternalAlter(context, null, null, FilterScope.OnAuthorityReceived, null!);

            Assert.NotNull(context.ProxyConfiguration);
            Assert.Equal("192.168.1.9", context.ProxyConfiguration!.Host);
            Assert.Equal(8080, context.ProxyConfiguration.Port);
        }

        [Theory]
        [InlineData("localhost")]
        [InlineData("api.internal.lan")]
        public async Task InternalAlter_Leaves_Direct_When_Host_Bypassed(string hostName)
        {
            var action = new UpStreamProxyAction("192.168.1.9", 8080) {
                ByPassHosts = { "localhost", "*.internal.lan" }
            };

            var context = CreateContext(hostName);

            await action.InternalAlter(context, null, null, FilterScope.OnAuthorityReceived, null!);

            Assert.Null(context.ProxyConfiguration);
        }

        [Fact]
        public void Serialization_Roundtrip_Preserves_ByPassHosts()
        {
            var parser = new RuleConfigParser();

            var action = new UpStreamProxyAction("192.168.1.9", 8080) {
                ProxyAuthorizationHeader = "Basic bGVlbG9vOm11bHRpcGFzcw==",
                ByPassHosts = { "localhost", "*.internal.lan" }
            };

            var rule = new Rule(action, new AnyFilter());

            var yaml = parser.GetYamlFromRule(rule);

            var outputRule = parser.TryGetRuleFromYaml(yaml, out var errors)!;

            Assert.True(errors == null || errors.Count == 0);

            var outputAction = (UpStreamProxyAction) outputRule.GetAllActions().Single();

            Assert.Equal("192.168.1.9", outputAction.Host);
            Assert.Equal(8080, outputAction.Port);
            Assert.Equal("Basic bGVlbG9vOm11bHRpcGFzcw==", outputAction.ProxyAuthorizationHeader);
            Assert.Equal(new[] { "localhost", "*.internal.lan" }, outputAction.ByPassHosts);
        }

        [Fact]
        public void Reading_Legacy_Config_Without_ByPassHosts_Yields_Empty_List()
        {
            var parser = new RuleConfigParser();

            // Pre-existing rule files do not carry the byPassHosts key.
            var yaml = """
                filter:
                  typeKind: AnyFilter
                action:
                  typeKind: UpStreamProxyAction
                  host: 192.168.1.9
                  port: 8080
                """;

            var rule = parser.TryGetRuleFromYaml(yaml, out var errors)!;

            Assert.True(errors == null || errors.Count == 0);

            var action = (UpStreamProxyAction) rule.GetAllActions().Single();

            Assert.Equal("192.168.1.9", action.Host);
            Assert.Equal(8080, action.Port);
            Assert.NotNull(action.ByPassHosts);
            Assert.Empty(action.ByPassHosts);
        }

        private static ExchangeContext CreateContext(string hostName)
        {
            var authority = new Authority(hostName, 443, true);

            return new ExchangeContext(authority, new VariableContext(), null,
                new SetUserAgentActionMapping(null));
        }
    }
}
