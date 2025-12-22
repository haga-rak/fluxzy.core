// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.Cli
{
    public class WithRuleOptionDelayAction : WithRuleOptionBase
    {
        [Fact]
        public async Task Validate_DelayAction()
        {
            // Arrange
            var yamlContent = $"""
                               rules:
                               - filter:
                                   typeKind: AuthorityFilter
                                   pattern: sandbox.fluxzy.io
                                   port: 443
                                   operation: exact
                                 action :
                                   typeKind: delayAction
                                   duration: 1000
                               """;

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                TestConstants.TestDomain);

            // Act
            var response = await Exec(yamlContent, requestMessage, allowAutoRedirect: false);
        }
    }
}
