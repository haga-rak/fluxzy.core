// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Web;
using Fluxzy.Rules.Filters;
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
        
        [Theory]
        [InlineData("coco", "lolo")]
        [InlineData("validcookiename", "/*-/8è?=;78")]
        public async Task Validate_SetResponseCookie(string name, string value)
        {
            // Arrange
            var yamlContent = $"""
                rules:
                - filter: 
                    typeKind: HostFilter  
                    pattern: {TestConstants.HttpBinHostDomainOnly}
                    operation: endsWith
                  action : 
                    typeKind: setResponseCookieAction
                    name: "{name}"
                    value: "{value}"
                """;

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"https://{TestConstants.HttpBinHost}/cookies");

            // Act
            using var response = await Exec(yamlContent, requestMessage);

            await response.Content.ReadAsStringAsync();

            var cookieCollection = CookieContainer.GetAllCookies();

            var cookie = cookieCollection[name];

            // Assert
            Assert.NotNull(cookie);
            Assert.Single(cookieCollection);
            Assert.Equal(value, HttpUtility.UrlDecode(cookie.Value));
            Assert.Single(cookieCollection);
        }

        [Theory]
        [InlineData("coco", "lolo", "/myPath", TestConstants.HttpBinHostDomainOnly, 3600, 3600, true, false, "None")]
        [InlineData("cocods", "lolo", "/", TestConstants.HttpBinHostDomainOnly, 3600, 3600, true, true, "None")]
        [InlineData("cocos", "lolo", "/sdfsf/", TestConstants.HttpBinHostDomainOnly, 3600, 3600, false, false, "None")]
        public async Task Validate_SetResponseCookie_With_Properties(
            string name, string value,
            string path, string domain, int expiresSeconds, int maxAgeSeconds, bool secure, bool httpOnly,
            string sameSite)
        {
            // Arrange
            var yamlContent = $"""
                rules:
                - filter: 
                    typeKind: HostFilter  
                    pattern: {TestConstants.HttpBinHostDomainOnly}
                    operation: endsWith
                  action : 
                    typeKind: setResponseCookieAction
                    name: "{name}"
                    value: "{value}"
                    path: "{path}"
                    domain: "{domain}"
                    expiresInSeconds: {expiresSeconds}
                    maxAge: {maxAgeSeconds}
                    secure: {secure}
                    httpOnly: {httpOnly}
                    sameSite: {sameSite}
                """;

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"https://{TestConstants.HttpBinHost}/cookies");

            // Act
            using var response = await Exec(yamlContent, requestMessage);

            await response.Content.ReadAsStringAsync();

            var cookieCollection = CookieContainer.GetAllCookies();

            var cookie = cookieCollection[name];

            // Assert
            Assert.NotNull(cookie);
            Assert.Single(cookieCollection);
            Assert.Equal(value, HttpUtility.UrlDecode(cookie.Value));
            Assert.Equal(path, cookie.Path);
            Assert.Equal(domain, cookie.Domain);
            Assert.Equal(secure, cookie.Secure);
            Assert.Equal(httpOnly, cookie.HttpOnly);
        }

        [Fact]
        public async Task Validate_HasSetCookieOnResponseFilter_Empty_Query()
        {
            // Arrange
            var yamlContent = $"""
                rules:
                - filter: 
                    typeKind: hasSetCookieOnResponseFilter
                  action : 
                    typeKind: addResponseHeaderAction
                    headerName: Passed
                    headerValue: true
                """;

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"https://{TestConstants.HttpBinHost}/cookies/set/coco/lolo");

            // Act
            using var response = await Exec(yamlContent, requestMessage, allowAutoRedirect:false);
            await response.Content.ReadAsStringAsync();

            // Assert
            Assert.True(response.Headers.TryGetValues("Passed", out var values));
        }

        [Fact]
        public async Task Validate_HasSetCookieOnResponseFilter_Name_Only()
        {
            // Arrange
            var yamlContent = $"""
                rules:
                - filter: 
                    typeKind: hasSetCookieOnResponseFilter
                    name: coco
                  action : 
                    typeKind: addResponseHeaderAction
                    headerName: Passed
                    headerValue: true
                """;

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"https://{TestConstants.HttpBinHost}/cookies/set/coco/lolo");

            // Act
            using var response = await Exec(yamlContent, requestMessage, allowAutoRedirect:false);
            await response.Content.ReadAsStringAsync();

            // Assert
            Assert.True(response.Headers.TryGetValues("Passed", out var values));
        }

        [Fact]
        public async Task Validate_HasSetCookieOnResponseFilter_Name_Only_Not_Pass()
        {
            // Arrange
            var yamlContent = $"""
                rules:
                - filter: 
                    typeKind: hasSetCookieOnResponseFilter
                    name: roro
                  action : 
                    typeKind: addResponseHeaderAction
                    headerName: Passed
                    headerValue: true
                """;

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"https://{TestConstants.HttpBinHost}/cookies/set/coco/lolo");

            // Act
            using var response = await Exec(yamlContent, requestMessage, allowAutoRedirect:false);
            await response.Content.ReadAsStringAsync();

            // Assert
            Assert.False(response.Headers.TryGetValues("Passed", out var values));
        }

        [Theory]
        [InlineData("coco", "lolo", StringSelectorOperation.Exact, "coco", "lolo", true)]
        [InlineData("codco", "lolo", StringSelectorOperation.Exact, "coco", "lolo", false)]
        [InlineData(null, "lolo", StringSelectorOperation.Exact, "coco", "lolo", true)]
        [InlineData(null, "lo", StringSelectorOperation.Contains, "coco", "lolo", true)]
        [InlineData("CAMPAIGNS", "{", StringSelectorOperation.Contains, "CAMPAIGNS", "aaa{", true)]
        [InlineData("coco", null, StringSelectorOperation.Exact, "coco", "lolo", true)]
        public async Task Validate_HasSetCookieOnResponseFilter_Generic(
            string? name, string? value, StringSelectorOperation operation,
            string expectedName, string expectedValue, bool result)
        {
            // Arrange
            var yamlContent = $"""
                rules:
                - filter: 
                    typeKind: hasSetCookieOnResponseFilter
                    name: "{name}" 
                    value: "{value}"
                    operation: {operation}
                  action : 
                    typeKind: addResponseHeaderAction
                    headerName: Passed
                    headerValue: true
                """;

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"https://{TestConstants.HttpBinHost}/cookies/set/{expectedName}/{expectedValue}");

            // Act
            using var response = await Exec(yamlContent, requestMessage, allowAutoRedirect: false);
            await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(result, response.Headers.TryGetValues("Passed", out var values));
        }


        public class HttpBinCookieResult
        {
            [JsonPropertyName("cookies")]
            public Dictionary<string, string> Cookies { get; set; } = new();
        }

        private static Dictionary<string, string> GetCookies(string responseBody)
        {
            var httpBinCookieResult = JsonSerializer.Deserialize<HttpBinCookieResult>(responseBody)!;
            return httpBinCookieResult.Cookies;
        }
    }
}
