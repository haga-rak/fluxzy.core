using System.Net.Http;
using System.Text;
using Fluxzy.Rules.Filters.RequestFilters;

namespace Fluxzy.Tests.Cli
{
    public class WithRuleOptionHasRequestBodyFilter : WithRuleOptionGenericRequestFilters
    {
        protected override string YamlContent { get; } = $"""
                rules:
                - filter: 
                    typeKind: {nameof(HasRequestBodyFilter)}
                """;

        protected override void ConfigurePass(HttpRequestMessage requestMessage)
        {
            requestMessage.Content = new StringContent("RandomValue", Encoding.UTF8, "text/plain");
            requestMessage.Method = HttpMethod.Post;
        }

        protected override void ConfigureBlock(HttpRequestMessage requestMessage)
        {
        }
    }
}