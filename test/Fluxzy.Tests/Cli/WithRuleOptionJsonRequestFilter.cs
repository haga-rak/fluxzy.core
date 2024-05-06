using System.Net.Http;
using System.Text;
using Fluxzy.Rules.Filters.RequestFilters;

namespace Fluxzy.Tests.Cli
{
    public class WithRuleOptionJsonRequestFilter : WithRuleOptionGenericRequestFilters
    {
        protected override string YamlContent { get; } = $"""
                rules:
                - filter: 
                    typeKind: {nameof(JsonRequestFilter)}
                """;

        protected override void ConfigurePass(HttpRequestMessage requestMessage)
        {
            requestMessage.Content = new StringContent("{}", Encoding.UTF8, "application/json");
            requestMessage.Method = HttpMethod.Post;
        }

        protected override void ConfigureBlock(HttpRequestMessage requestMessage)
        {
            requestMessage.Content = new StringContent("sd{}", Encoding.UTF8, "text/plain");
            requestMessage.Method = HttpMethod.Post;
        }
    }
}
