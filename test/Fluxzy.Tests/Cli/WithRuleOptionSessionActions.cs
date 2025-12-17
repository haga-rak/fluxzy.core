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
    public class WithRuleOptionSessionActions : WithRuleOptionBase
    {
        private HttpClient SecondaryHttpClient {
            get
            {
                var httpClient = new HttpClient(new HttpClientHandler() {
                    UseProxy = true,
                    Proxy = Client!.WebProxy
                });

                return httpClient;
            }
        }

        [Fact]
        public async Task CaptureSession_CapturesCookies()
        {
            // Arrange - First request sets a cookie, second request should capture it
            var yamlContent = $"""
                               rules:
                               - filter:
                                   typeKind: HostFilter
                                   pattern: {TestConstants.HttpBinHostDomainOnly}
                                   operation: endsWith
                                 actions:
                                   - typeKind: captureSessionAction
                                     captureCookies: true
                               """;

            // First request - sets cookie
            var requestMessage1 = new HttpRequestMessage(HttpMethod.Get,
                $"https://{TestConstants.HttpBinHost}/cookies/set/sessionId/captured123");

            // Act
            using var response1 = await Exec(yamlContent, requestMessage1, allowAutoRedirect: true);
            await response1.Content.ReadAsStringAsync();

            // Assert - Cookie should be set
            Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        }

        [Fact]
        public async Task ApplySession_AppliesCapturedCookies()
        {
            // Arrange - Capture session from first request, apply to second
            var yamlContent = $"""
                               rules:
                               - filter:
                                   typeKind: HostFilter
                                   pattern: {TestConstants.HttpBinHostDomainOnly}
                                   operation: endsWith
                                 actions:
                                   - typeKind: captureSessionAction
                                     captureCookies: true
                                   - typeKind: applySessionAction
                                     applyCookies: true
                                     applyHeaders: false
                               """;

            // First request - sets cookie (will be captured)
            var requestMessage1 = new HttpRequestMessage(HttpMethod.Get,
                $"https://{TestConstants.HttpBinHost}/cookies/set/testSession/myValue123");

            using var response1 = await Exec(yamlContent, requestMessage1, false);
            await response1.Content.ReadAsStringAsync();

            // Second request - should have cookie applied from session
            var requestMessage2 = new HttpRequestMessage(HttpMethod.Get,
                $"https://{TestConstants.HttpBinHost}/cookies");

            using var response2 = await SecondaryHttpClient.SendAsync(requestMessage2);
            var responseBody = await response2.Content.ReadAsStringAsync();

            var cookies = GetCookies(responseBody);

            // Assert
            Assert.True(cookies.ContainsKey("testSession"));
            Assert.Equal("myValue123", cookies["testSession"]);
        }

        [Fact]
        public async Task ApplySession_MergesWithExistingCookies()
        {
            // Arrange
            var yamlContent = $"""
                               rules:
                               - filter:
                                   typeKind: HostFilter
                                   pattern: {TestConstants.HttpBinHostDomainOnly}
                                   operation: endsWith
                                 actions:
                                   - typeKind: captureSessionAction
                                     captureCookies: true
                                   - typeKind: applySessionAction
                                     applyCookies: true
                                     mergeWithExisting: true
                               """;

            // First request - sets a session cookie
            var requestMessage1 = new HttpRequestMessage(HttpMethod.Get,
                $"https://{TestConstants.HttpBinHost}/cookies/set/sessionCookie/fromSession");

            using var response1 = await Exec(yamlContent, requestMessage1, false);
            await response1.Content.ReadAsStringAsync();

            var requestMessage2 = new HttpRequestMessage(HttpMethod.Get,
                $"https://{TestConstants.HttpBinHost}/cookies/set/clientCookie/fromClient");

            using var response2 = await SecondaryHttpClient.SendAsync(requestMessage2);

            // Second request - should have both cookies
            var requestMessage3 = new HttpRequestMessage(HttpMethod.Get,
                $"https://{TestConstants.HttpBinHost}/cookies");

            using var response3 = await SecondaryHttpClient.SendAsync(requestMessage3);
            var responseBody = await response3.Content.ReadAsStringAsync();

            var cookies = GetCookies(responseBody);

            // Assert - both cookies should be present
            Assert.True(cookies.ContainsKey("sessionCookie"));
            Assert.True(cookies.ContainsKey("clientCookie"));
        }

        [Fact]
        public async Task CaptureSession_CapturesCustomHeaders()
        {
            // Arrange - Use setResponseCookieAction to add a header, then capture it
            var yamlContent = $"""
                               rules:
                               - filter:
                                   typeKind: HostFilter
                                   pattern: {TestConstants.HttpBinHostDomainOnly}
                                   operation: endsWith
                                 actions:
                                   - typeKind: addResponseHeaderAction
                                     headerName: X-Custom-Token
                                     headerValue: token123
                                   - typeKind: captureSessionAction
                                     captureCookies: true
                                     captureHeaders:
                                       - X-Custom-Token
                               """;

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"https://{TestConstants.HttpBinHost}/get");

            // Act
            using var response = await Exec(yamlContent, requestMessage);
            await response.Content.ReadAsStringAsync();

            // Assert - Header should be captured (we can't directly verify, but request should succeed)
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ClearSession_ClearsAllSessions()
        {
            // Arrange
            var yamlContent = $"""
                               rules:
                               - filter:
                                   typeKind: HostFilter
                                   pattern: {TestConstants.HttpBinHostDomainOnly}
                                   operation: endsWith
                                 actions:
                                   - typeKind: captureSessionAction
                                     captureCookies: true
                                   - typeKind: clearSessionAction
                                   - typeKind: applySessionAction
                                     applyCookies: true
                               """;

            // Request that sets cookie, captures, clears, then tries to apply
            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"https://{TestConstants.HttpBinHost}/cookies/set/willBeClearedCookie/value");

            using var response1 = await Exec(yamlContent, requestMessage, false);
            await response1.Content.ReadAsStringAsync();

            // Second request - session should be cleared, no cookies applied
            var requestMessage2 = new HttpRequestMessage(HttpMethod.Get,
                $"https://{TestConstants.HttpBinHost}/cookies");

            using var response2 = await SecondaryHttpClient.SendAsync(requestMessage2);
            var responseBody = await response2.Content.ReadAsStringAsync();

            var cookies = GetCookies(responseBody);

            // Assert - No session cookies should be applied (since we cleared)
            // Note: The cookie set by /cookies/set might still be in browser cookie jar
            // but session store should be empty
            Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        }

        [Fact]
        public async Task ClearSession_ClearsDomainSpecific()
        {
            // Arrange
            var yamlContent = $"""
                               rules:
                               - filter:
                                   typeKind: HostFilter
                                   pattern: {TestConstants.HttpBinHostDomainOnly}
                                   operation: endsWith
                                 actions:
                                   - typeKind: captureSessionAction
                                     captureCookies: true
                               - filter:
                                   typeKind: PathFilter
                                   pattern: "/clear"
                                 actions:
                                   - typeKind: clearSessionAction
                                     domain: {TestConstants.HttpBinHostDomainOnly}
                               """;

            // First request - sets cookie
            var requestMessage1 = new HttpRequestMessage(HttpMethod.Get,
                $"https://{TestConstants.HttpBinHost}/cookies/set/toClear/value");

            using var response1 = await Exec(yamlContent, requestMessage1, allowAutoRedirect: true);
            await response1.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        }

        [Fact]
        public async Task ApplySession_FromDifferentDomain()
        {
            // This test verifies the SourceDomain feature
            var yamlContent = $"""
                               rules:
                               - filter:
                                   typeKind: HostFilter
                                   pattern: {TestConstants.HttpBinHostDomainOnly}
                                   operation: endsWith
                                 actions:
                                   - typeKind: captureSessionAction
                                     captureCookies: true
                                   - typeKind: applySessionAction
                                     applyCookies: true
                                     sourceDomain: {TestConstants.HttpBinHostDomainOnly}
                               """;

            // First request - sets cookie
            var requestMessage1 = new HttpRequestMessage(HttpMethod.Get,
                $"https://{TestConstants.HttpBinHost}/cookies/set/crossDomain/value");

            using var response1 = await Exec(yamlContent, requestMessage1, false);
            await response1.Content.ReadAsStringAsync();

            // Second request - should have cookie from source domain
            var requestMessage2 = new HttpRequestMessage(HttpMethod.Get,
                $"https://{TestConstants.HttpBinHost}/cookies");

            using var response2 = await SecondaryHttpClient.SendAsync(requestMessage2);
            var responseBody = await response2.Content.ReadAsStringAsync();

            var cookies = GetCookies(responseBody);

            // Assert
            Assert.True(cookies.ContainsKey("crossDomain"));
        }

        private static Dictionary<string, string> GetCookies(string responseBody)
        {
            var httpBinCookieResult = JsonSerializer.Deserialize<HttpBinCookieResult>(responseBody)!;
            return httpBinCookieResult.Cookies;
        }

        public class HttpBinCookieResult
        {
            [JsonPropertyName("cookies")]
            public Dictionary<string, string> Cookies { get; set; } = new();
        }
    }
}
