using System.Net.Http;
using Fluxzy.Rules.Filters.RequestFilters;

namespace Fluxzy.Tests.Cli
{
    public class WithRuleOptionH2PutFilter : WithRuleOptionGenericRequestFilters
    {
        protected override string YamlContent { get; } = $"""
                rules:
                - filter: 
                    typeKind: {nameof(PutFilter)}
                """;

        protected override void ConfigurePass(HttpRequestMessage requestMessage)
        {
            requestMessage.Method = HttpMethod.Put;
        }

        protected override void ConfigureBlock(HttpRequestMessage requestMessage)
        {
        }
    }
}