using System.Net.Http;
using System.Text;
using Fluxzy.Rules.Filters.RequestFilters;

namespace Fluxzy.Tests.Cli
{
    public class WithRuleOptionProtobufRequestFilter : WithRuleOptionGenericRequestFilters
    {
        protected override string YamlContent { get; } = $"""
                rules:
                - filter:
                    typeKind: {nameof(ProtobufRequestFilter)}
                """;

        protected override void ConfigurePass(HttpRequestMessage requestMessage)
        {
            requestMessage.Content = new StringContent("\x00", Encoding.UTF8, "application/x-protobuf");
            requestMessage.Method = HttpMethod.Post;
        }

        protected override void ConfigureBlock(HttpRequestMessage requestMessage)
        {
            requestMessage.Content = new StringContent("{}", Encoding.UTF8, "application/json");
            requestMessage.Method = HttpMethod.Post;
        }
    }
}
