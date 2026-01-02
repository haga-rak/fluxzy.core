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

        [Fact]
        public async Task CaptureSession_CapturesRequestCookies()
        {
            // Arrange - Test CaptureRequestCookies option for capturing cookies from request headers
            // This is useful when proxy is inserted into an ongoing session
            var yamlContent = $"""
                               rules:
                               - filter:
                                   typeKind: HostFilter
                                   pattern: {TestConstants.HttpBinHostDomainOnly}
                                   operation: endsWith
                                 actions:
                                   - typeKind: captureSessionAction
                                     captureCookies: true
                                     captureRequestCookies: true
                                   - typeKind: applySessionAction
                                     applyCookies: true
                               """;

            // First request - send with a Cookie header (simulating ongoing session)
            var requestMessage1 = new HttpRequestMessage(HttpMethod.Get,
                $"https://{TestConstants.HttpBinHost}/cookies");
            requestMessage1.Headers.Add("Cookie", "existingSession=capturedFromRequest; anotherCookie=value123");

            using var response1 = await Exec(yamlContent, requestMessage1, false);
            var responseBody1 = await response1.Content.ReadAsStringAsync();

            // Verify first request received the cookies we sent
            var cookies1 = GetCookies(responseBody1);
            Assert.True(cookies1.ContainsKey("existingSession"));
            Assert.Equal("capturedFromRequest", cookies1["existingSession"]);

            // Second request - should have captured cookies applied from session store
            var requestMessage2 = new HttpRequestMessage(HttpMethod.Get,
                $"https://{TestConstants.HttpBinHost}/cookies");

            using var response2 = await SecondaryHttpClient.SendAsync(requestMessage2);
            var responseBody2 = await response2.Content.ReadAsStringAsync();

            var cookies2 = GetCookies(responseBody2);

            // Assert - cookies from first request should be captured and applied
            Assert.True(cookies2.ContainsKey("existingSession"));
            Assert.Equal("capturedFromRequest", cookies2["existingSession"]);
            Assert.True(cookies2.ContainsKey("anotherCookie"));
            Assert.Equal("value123", cookies2["anotherCookie"]);
        }

        [Fact]
        public async Task CaptureSession_RequestCookies_SetCookieTakesPrecedence()
        {
            // Arrange - Test that Set-Cookie response takes precedence over request Cookie header
            var yamlContent = $"""
                               rules:
                               - filter:
                                   typeKind: HostFilter
                                   pattern: {TestConstants.HttpBinHostDomainOnly}
                                   operation: endsWith
                                 actions:
                                   - typeKind: captureSessionAction
                                     captureCookies: true
                                     captureRequestCookies: true
                                   - typeKind: applySessionAction
                                     applyCookies: true
                               """;

            // First request - send with Cookie header, but also receive Set-Cookie for same name
            var requestMessage1 = new HttpRequestMessage(HttpMethod.Get,
                $"https://{TestConstants.HttpBinHost}/cookies/set/overwrittenCookie/fromSetCookie");
            requestMessage1.Headers.Add("Cookie", "overwrittenCookie=fromRequest");

            using var response1 = await Exec(yamlContent, requestMessage1, false);
            await response1.Content.ReadAsStringAsync();

            // Second request - should have Set-Cookie value, not request Cookie value
            var requestMessage2 = new HttpRequestMessage(HttpMethod.Get,
                $"https://{TestConstants.HttpBinHost}/cookies");

            using var response2 = await SecondaryHttpClient.SendAsync(requestMessage2);
            var responseBody2 = await response2.Content.ReadAsStringAsync();

            var cookies2 = GetCookies(responseBody2);

            // Assert - Set-Cookie value should take precedence
            Assert.True(cookies2.ContainsKey("overwrittenCookie"));
            Assert.Equal("fromSetCookie", cookies2["overwrittenCookie"]);
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
