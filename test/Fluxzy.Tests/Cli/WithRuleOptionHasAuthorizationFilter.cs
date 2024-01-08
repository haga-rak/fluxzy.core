using System.Net.Http;

namespace Fluxzy.Tests.Cli
{
    public class WithRuleOptionHasAuthorizationFilter : WithRuleOptionGenericRequestFilters
    {
        protected override string YamlContent { get; } = """
                rules:
                - filter: 
                    typeKind: hasAuthorizationFilter
                """;

        protected override void ConfigurePass(HttpRequestMessage requestMessage)
        {
            requestMessage.Headers.Add("Authorization", "ds");
        }

        protected override void ConfigureBlock(HttpRequestMessage requestMessage)
        {
        }
    }
}