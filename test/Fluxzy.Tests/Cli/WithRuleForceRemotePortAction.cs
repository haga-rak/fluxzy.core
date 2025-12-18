// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Fluxzy.Tests.Cli
{
    public class WithRuleForceRemotePortAction : WithRuleOptionBase
    {

        [Theory]
        [InlineData(4564)]
        [InlineData(1243)]
        public async Task Validate(int invalidPortNumber)
        {
            // Arrange
            var yamlContent = $"""
                               rules:
                               - filter:
                                   typeKind: anyFilter
                                 action :
                                   typeKind: forceRemotePortAction
                                   port: 5001
                               """;

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"https://sandbox.fluxzy.io:{invalidPortNumber}/ip");

            // Act
            using var response = await Exec(yamlContent, requestMessage, allowAutoRedirect: false);
            await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        }
    }
}
