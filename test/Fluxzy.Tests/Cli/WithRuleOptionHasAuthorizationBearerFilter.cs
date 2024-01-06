// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net.Http;

namespace Fluxzy.Tests.Cli
{
    public class WithRuleOptionHasAuthorizationBearerFilter : WithRuleOptionGenericRequestFilters
    {
        protected override string YamlContent { get; } = """
                rules:
                - filter: 
                    typeKind: hasAuthorizationBearerFilter
                """;

        protected override void ConfigurePass(HttpRequestMessage requestMessage)
        {
            requestMessage.Headers.Add("Authorization", "Bearer RandomValue");
        }

        protected override void ConfigureBlock(HttpRequestMessage requestMessage)
        {
        }
    }
}
