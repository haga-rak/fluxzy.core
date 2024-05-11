// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Net.Http;
using Fluxzy.Rules.Filters.RequestFilters;

namespace Fluxzy.Tests.Cli
{
    public class WithRuleOptionMultipartDataRequestFilter: WithRuleOptionGenericRequestFilters
    {
        protected override string YamlContent { get; } = $"""
                                                          rules:
                                                          - filter:
                                                              typeKind: {nameof(MultipartDataRequestFilter)}
                                                          """;

        protected override void ConfigurePass(HttpRequestMessage requestMessage)
        {
            requestMessage.Content = new MultipartFormDataContent
            {
                {new StringContent("value"), "name"}
            };
            
            requestMessage.Method = HttpMethod.Post;
        }

        protected override void ConfigureBlock(HttpRequestMessage requestMessage)
        {
            requestMessage.Content = new FormUrlEncodedContent(new Dictionary<string, string>() {
                ["name"] = "value"
            }); 
            requestMessage.Method = HttpMethod.Post;
        }
    }
}
