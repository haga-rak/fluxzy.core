// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.Cli
{
    public class WithRuleAddAuthorizationBasicAction : WithRuleOptionBase
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
                                   - typeKind: addAuthorizationBasicAction
                                     user: user1
                                     password: pass1
                               """;

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"{TestConstants.Http11Host}/global-health-check");

            // Act
            using var response = await Exec(yamlContent, requestMessage, allowAutoRedirect: false);
            var checkResult = await response.GetCheckResult();
            var authorizationHeader =
                checkResult.Headers?
                    .SingleOrDefault(h => h.Name.Equals("authorization", StringComparison.OrdinalIgnoreCase));

            // assert
            var expectedValue = $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes("user1:pass1"))}";

            Assert.NotNull(authorizationHeader);
            Assert.NotNull(authorizationHeader.Value);
            Assert.Equal(expectedValue, authorizationHeader.Value);
        }
    }
}
