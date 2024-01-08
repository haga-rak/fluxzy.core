using System;
using System.Net.Http;
using Fluxzy.Tests._Fixtures;

namespace Fluxzy.Tests.Cli
{
    public class WithRuleOptionH11TrafficOnlyFilter : WithRuleOptionGenericRequestFilters
    {
        protected override string YamlContent { get; } = """
                rules:
                - filter: 
                    typeKind: h11TrafficOnlyFilter
                """;

        protected override void ConfigurePass(HttpRequestMessage requestMessage)
        {
            requestMessage.RequestUri = new Uri($"{TestConstants.Http11Host}/global-health-check");
        }

        protected override void ConfigureBlock(HttpRequestMessage requestMessage)
        {
        }
    }
}