// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Web;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.Cli
{
    public class WithRuleOptionCookies : WithRuleOptionBase
    {
        [Theory]
        [InlineData("coco", "lolo")]
        [InlineData("coco", "")]
        [InlineData("/8è?=;78", "/*-/8è?=;78")]
        public async Task Validate_SetRequestCookie(string name, string value)
        {
            // Arrange
            var yamlContent = $"""
                rules:
                - filter: 
                    typeKind: HostFilter  
                    pattern: {TestConstants.HttpBinHostDomainOnly}
                    operation: endsWith
                  action : 
                    typeKind: setRequestCookieAction
                    name: "{name}"
                    value: "{value}"
                """;

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"https://{TestConstants.HttpBinHost}/cookies");

            // Act 
            using var response = await Exec(yamlContent, requestMessage);

            var responseBody = await response.Content.ReadAsStringAsync();

            var cookies = GetCookies(responseBody)
                .ToDictionary(t => HttpUtility.UrlDecode(t.Key), t => HttpUtility.UrlDecode(t.Value));

            // Assert
            Assert.Equal(value, cookies[name]);
            Assert.Single(cookies); 
        }

        [Fact]
        public async Task Validate_SetRequestCookie_With_Existing()
        {
            // Arrange
            var yamlContent = $"""
                rules:
                - filter: 
                    typeKind: HostFilter  
                    pattern: {TestConstants.HttpBinHostDomainOnly}
                    operation: endsWith
                  action : 
                    typeKind: setRequestCookieAction
                    name: coco
                    value: lolo
                """;

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"https://{TestConstants.HttpBinHost}/cookies");

            CookieContainer.Add(new Cookie("coco", "existing_Value", "/", TestConstants.HttpBinHostDomainOnly));
            CookieContainer.Add(new Cookie("anotherCookie", "yes", "/", TestConstants.HttpBinHostDomainOnly));

            // Act 
            using var response = await Exec(yamlContent, requestMessage);

            var responseBody = await response.Content.ReadAsStringAsync();

            var cookies = GetCookies(responseBody);

            // Assert
            Assert.Equal("lolo", cookies["coco"]);
            Assert.Equal(2, cookies.Count); 
        }

        public class HttpBinCookieResult
        {
            [JsonPropertyName("cookies")]
            public Dictionary<string, string> Cookies { get; set; }
        }

        private static Dictionary<string, string> GetCookies(string responseBody)
        {
            var httpBinCookieResult = JsonSerializer.Deserialize<HttpBinCookieResult>(responseBody)!;
            return httpBinCookieResult.Cookies;
        }
    }
}
