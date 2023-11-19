// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Fluxzy.Tests.Cli
{
    public class WithRuleOptionAbortAction : WithRuleOptionBase
    {
        [Fact]
        public async Task Validate_AbortAction()
        {
            // Arrange
            var yamlContent = $"""
                               rules:
                               - filter:
                                   typeKind: AuthorityFilter
                                   pattern: www.example.com
                                   port: 443
                                   operation: exact
                                 action :
                                   typeKind: abortAction
                               """;

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"https://www.example.com/");

            // Act
            var responsePromise = Exec(yamlContent, requestMessage, allowAutoRedirect: false);

            // Assert
            await Assert.ThrowsAsync<HttpRequestException>(async () => {
                using var response = await responsePromise;
            });
        }
    }
}
