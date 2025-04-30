// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
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

        [Theory]
        [InlineData("https://sandbox.smartizy.com:5001")]
        public async Task Validate_With_Query_String(string urlHost)
        {
            var path = "/global-health-check?bar=boo";
            var expectedFinalUrl = $"{urlHost}{path}";

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
                $"https://www.example.com{path}");

            // Act
            using var response = await Exec(yamlContent, requestMessage, allowAutoRedirect: false);
            var responseText = await response.Content.ReadAsStringAsync();
            var checkResponse = JsonSerializer.Deserialize<GlobalCheckResponse>(responseText);

            Assert.NotNull(checkResponse);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedFinalUrl, checkResponse.Url);
        }
    }

    class GlobalCheckResponse
    {
        public GlobalCheckResponse(string url)
        {
            Url = url;
        }

        [JsonPropertyName("url")]
        public string Url { get; }
    }

}
