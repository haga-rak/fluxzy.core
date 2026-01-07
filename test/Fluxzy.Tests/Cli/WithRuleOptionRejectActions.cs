// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Fluxzy.Tests.Cli
{
    public class WithRuleOptionRejectActions : WithRuleOptionBase
    {
        [Fact]
        public async Task Validate_RejectAction_Returns_403_Forbidden()
        {
            // Arrange
            var yamlContent = """
                rules:
                - filter:
                    typeKind: anyFilter
                  action:
                    typeKind: rejectAction
                """;

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                "https://blocked-host-reject-test.com/resource");

            // Act
            using var response = await Exec(yamlContent, requestMessage);
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal("Forbidden", responseBody);
        }

        [Fact]
        public async Task Validate_RejectWithStatusCodeAction_Returns_404()
        {
            // Arrange
            var yamlContent = """
                rules:
                - filter:
                    typeKind: anyFilter
                  action:
                    typeKind: rejectWithStatusCodeAction
                    statusCode: 404
                """;

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                "https://blocked-host-reject-test.com/hidden-resource");

            // Act
            using var response = await Exec(yamlContent, requestMessage);
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("NotFound", responseBody);
        }

        [Fact]
        public async Task Validate_RejectWithStatusCodeAction_Returns_502()
        {
            // Arrange
            var yamlContent = """
                rules:
                - filter:
                    typeKind: anyFilter
                  action:
                    typeKind: rejectWithStatusCodeAction
                    statusCode: 502
                """;

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                "https://blocked-host-reject-test.com/unavailable");

            // Act
            using var response = await Exec(yamlContent, requestMessage);
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
            Assert.Equal("BadGateway", responseBody);
        }

        [Fact]
        public async Task Validate_RejectWithStatusCodeAction_Default_Is_403()
        {
            // Arrange
            var yamlContent = """
                rules:
                - filter:
                    typeKind: anyFilter
                  action:
                    typeKind: rejectWithStatusCodeAction
                """;

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                "https://blocked-host-reject-test.com/default");

            // Act
            using var response = await Exec(yamlContent, requestMessage);

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Validate_RejectWithMessageAction_PlainText()
        {
            // Arrange
            var yamlContent = """
                rules:
                - filter:
                    typeKind: anyFilter
                  action:
                    typeKind: rejectWithMessageAction
                    statusCode: 403
                    message: "Access denied by corporate policy"
                    contentType: text/plain
                """;

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                "https://blocked-host-reject-test.com/blocked");

            // Act
            using var response = await Exec(yamlContent, requestMessage);
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal("Access denied by corporate policy", responseBody);
            Assert.True(response.Content.Headers.TryGetValues("Content-Type", out var contentTypeValues));
            Assert.StartsWith("text/plain", contentTypeValues.First());
        }

        [Fact]
        public async Task Validate_RejectWithMessageAction_Html()
        {
            // Arrange
            var yamlContent = """
                rules:
                - filter:
                    typeKind: anyFilter
                  action:
                    typeKind: rejectWithMessageAction
                    statusCode: 403
                    message: "<html><body><h1>Blocked</h1></body></html>"
                    contentType: text/html
                """;

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                "https://blocked-host-reject-test.com/html-blocked");

            // Act
            using var response = await Exec(yamlContent, requestMessage);
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal("<html><body><h1>Blocked</h1></body></html>", responseBody);
            Assert.True(response.Content.Headers.TryGetValues("Content-Type", out var contentTypeValues));
            Assert.StartsWith("text/html", contentTypeValues.First());
        }

        [Fact]
        public async Task Validate_RejectWithMessageAction_Json()
        {
            // Arrange
            var yamlContent = """
                rules:
                - filter:
                    typeKind: anyFilter
                  action:
                    typeKind: rejectWithMessageAction
                    statusCode: 403
                    message: '{"error": "forbidden", "message": "This endpoint is blocked"}'
                    contentType: application/json
                """;

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                "https://blocked-host-reject-test.com/api/blocked");

            // Act
            using var response = await Exec(yamlContent, requestMessage);
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal("{\"error\": \"forbidden\", \"message\": \"This endpoint is blocked\"}", responseBody);
            Assert.True(response.Content.Headers.TryGetValues("Content-Type", out var contentTypeValues));
            Assert.StartsWith("application/json", contentTypeValues.First());
        }

        [Fact]
        public async Task Validate_RejectWithMessageAction_Default_Values()
        {
            // Arrange - only specifying typeKind, using all defaults
            var yamlContent = """
                rules:
                - filter:
                    typeKind: anyFilter
                  action:
                    typeKind: rejectWithMessageAction
                """;

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                "https://blocked-host-reject-test.com/defaults");

            // Act
            using var response = await Exec(yamlContent, requestMessage);
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal("Request blocked by proxy", responseBody);
        }

        [Fact]
        public async Task Validate_RejectWithMessageAction_Custom_StatusCode()
        {
            // Arrange
            var yamlContent = """
                rules:
                - filter:
                    typeKind: anyFilter
                  action:
                    typeKind: rejectWithMessageAction
                    statusCode: 451
                    message: "Unavailable For Legal Reasons"
                """;

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                "https://blocked-host-reject-test.com/legal");

            // Act
            using var response = await Exec(yamlContent, requestMessage);
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal((HttpStatusCode)451, response.StatusCode);
            Assert.Equal("Unavailable For Legal Reasons", responseBody);
        }

        [Fact]
        public async Task Validate_RejectAction_With_HostFilter()
        {
            // Arrange - Only block specific host
            var yamlContent = """
                rules:
                - filter:
                    typeKind: hostFilter
                    pattern: blocked-specific-host-reject-test.com
                  action:
                    typeKind: rejectAction
                """;

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                "https://blocked-specific-host-reject-test.com/resource");

            // Act
            using var response = await Exec(yamlContent, requestMessage);

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
