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
        [InlineData("http://sandbox.smartizy.com:8899")]
        [InlineData("http://sandbox.smartizy.com:8899/")]
        [InlineData("https://sandbox.smartizy.com")]
        [InlineData("https://sandbox.smartizy.com/")]
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
            Assert.Equal("HTTP/1.0", responseText);
        }
    }
}
