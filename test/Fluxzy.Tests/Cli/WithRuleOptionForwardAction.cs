// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Fluxzy.Tests.Cli
{
    public class WithRuleOptionForwardAction : WithRuleOptionBase
    {
        [Theory]
        [InlineData("http://sandbox.fluxzy.io:8899")]
        [InlineData("http://sandbox.fluxzy.io:8899/")]
        [InlineData("https://sandbox.fluxzy.io")]
        [InlineData("https://sandbox.fluxzy.io/")]
        public async Task Validate(string urlHost)
        {
            // Arrange
            var yamlContent = $"""
                               rules:
                               - filter:
                                   typeKind: AnyFilter
                                 action :
                                   typeKind: ForwardAction
                                   url: {urlHost}
                               """;

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"https://noexistingdomain7867443434334.com/protocol");

            // Act
            using var response = await Exec(yamlContent, requestMessage, allowAutoRedirect: false);
            var responseText = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("HTTP/1.1", responseText);
        }
    }
}
