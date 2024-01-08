using System;
using System.Net.Http;
using Fluxzy.Rules.Filters.RequestFilters;
using Fluxzy.Tests._Fixtures;

namespace Fluxzy.Tests.Cli
{
    public class WithRuleOptionH2TrafficOnlyFilter : WithRuleOptionGenericRequestFilters
    {
        protected override string YamlContent { get; } = $"""
                rules:
                - filter: 
                    typeKind: {nameof(H2TrafficOnlyFilter)}
                """;

        protected override void ConfigurePass(HttpRequestMessage requestMessage)
        {
        }

        protected override void ConfigureBlock(HttpRequestMessage requestMessage)
        {
            requestMessage.RequestUri = new Uri($"{TestConstants.Http11Host}/global-health-check");
        }
    }
}