// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.Cli
{
    public class WithRuleAddAuthorizationBearerAction : WithRuleOptionBase
    {
        [Fact]

        public async Task Validate()
        {
            // Arrange
            var yamlContent = $"""
                               rules:
                               - filter:
                                   typeKind: anyFilter
                                 actions :
                                   - typeKind: addAuthorizationBearerAction
                                     token: xyz
                               """;

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"{TestConstants.Http11Host}/global-health-check");

            // Act
            using var response = await Exec(yamlContent, requestMessage, allowAutoRedirect: false);

            var checkResult = await response.GetCheckResult();

            var authorizationHeader = 
                checkResult.Headers?
                    .SingleOrDefault(h => h.Name.Equals("authorization", StringComparison.OrdinalIgnoreCase));

            Assert.NotNull(authorizationHeader);
            Assert.NotNull(authorizationHeader.Value);

            Assert.Equal("Bearer xyz", authorizationHeader.Value);
        }
    }
}
