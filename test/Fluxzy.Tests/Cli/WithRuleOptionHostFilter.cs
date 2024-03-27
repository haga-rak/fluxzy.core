// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net.Http;

namespace Fluxzy.Tests.Cli
{
    public class WithRuleOptionHostFilter : WithRuleOptionGenericRequestFilters
    {
        protected override string YamlContent { get; } = """
                                                         rules:
                                                         - filter:
                                                             typeKind: HostFilter
                                                             pattern: sandbox.smartizy.com
                                                         """;

        protected override void ConfigurePass(HttpRequestMessage requestMessage)
        {
           
        }

        protected override void ConfigureBlock(HttpRequestMessage requestMessage)
        {
            requestMessage.RequestUri = new System.Uri("http://example.com");
        }
    }
}
