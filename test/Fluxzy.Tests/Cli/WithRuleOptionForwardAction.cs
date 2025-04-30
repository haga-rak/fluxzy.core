// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Fluxzy.Tests.Cli
{
    public class WithRuleOptionForwardAction : WithRuleOptionBase
    {
        [Fact]
        public async Task Validate_Simple()
        {
            // Arrange
            var yamlContent = $"""
                               rules:
                               - filter:
                                   typeKind: AnyFilter
                                 action :
                                   typeKind: ForwardAction
                                   url: http://sandbox.smartizy.com:8899/protocol
                               """;

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"https://noexistingdomain7867443434334.com");

            // Act
            using var response = await Exec(yamlContent, requestMessage, allowAutoRedirect: false);
            var responseText = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode);
            Assert.Equal("HTTP/1.0", responseText);
        }
    }
}
