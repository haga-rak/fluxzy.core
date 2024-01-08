using System.Net.Http;
using Fluxzy.Rules.Filters.RequestFilters;

namespace Fluxzy.Tests.Cli
{
    public class WithRuleOptionH2DeleteFilter : WithRuleOptionGenericRequestFilters
    {
        protected override string YamlContent { get; } = $"""
                rules:
                - filter: 
                    typeKind: {nameof(DeleteFilter)}
                """;

        protected override void ConfigurePass(HttpRequestMessage requestMessage)
        {
            requestMessage.Method = HttpMethod.Delete;
        }

        protected override void ConfigureBlock(HttpRequestMessage requestMessage)
        {
        }
    }
}