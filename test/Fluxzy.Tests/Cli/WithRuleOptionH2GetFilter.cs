using System.Net.Http;
using Fluxzy.Rules.Filters.RequestFilters;

namespace Fluxzy.Tests.Cli
{
    public class WithRuleOptionH2GetFilter : WithRuleOptionGenericRequestFilters
    {
        protected override string YamlContent { get; } = $"""
                rules:
                - filter: 
                    typeKind: {nameof(GetFilter)}
                """;

        protected override void ConfigurePass(HttpRequestMessage requestMessage)
        {
        }

        protected override void ConfigureBlock(HttpRequestMessage requestMessage)
        {
            requestMessage.Method = HttpMethod.Post;
        }
    }
}