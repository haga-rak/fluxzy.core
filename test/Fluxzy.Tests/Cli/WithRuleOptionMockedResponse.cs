// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Tests._Files;
using Xunit;

namespace Fluxzy.Tests.Cli
{
    public class WithRuleOptionMockedResponse : WithRuleOptionBase
    {
        [Fact]
        public async Task Validate_MockedResponse()
        {
            // Arrange
            var yamlContent = """
                rules:
                - filter: 
                    typeKind: anyFilter  
                    operation: endsWith
                  action : 
                    typeKind: mockedResponseAction
                    response:
                      statusCode: 201
                      body:
                        text: cocoyes
                """;

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"https://veryinvalidhost79795-sfsdfdsf.com/cookies");

            // Act 
            using var response = await Exec(yamlContent, requestMessage);

            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal("cocoyes", responseBody);
        }

        [Fact]
        public async Task Validate_MockedResponse_Short()
        {
            // Arrange
            var yamlContent = """
                rules:
                - filter: 
                    typeKind: anyFilter  
                    operation: endsWith
                  action : 
                    typeKind: mockedResponseAction
                    response:
                      statusCode: 204
                """;

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"https://veryinvalidhost79795-sfsdfdsf.com/cookies");

            // Act 
            using var response = await Exec(yamlContent, requestMessage);

            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal("", responseBody);
        }

        [Fact]
        public async Task Validate_MockedResponse_With_Header()
        {
            // Arrange
            var yamlContent = """
                rules:
                - filter: 
                    typeKind: anyFilter  
                    operation: endsWith
                  action : 
                    typeKind: mockedResponseAction
                    response:
                      statusCode: 200
                      headers:
                        - name: coco 
                          value: lolo 
                        - name: zozo 
                          value: zaza 
                      body:
                        text: cocoyes
                """;

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"https://veryinvalidhost79795-sfsdfdsf.com/cookies");

            // Act 
            using var response = await Exec(yamlContent, requestMessage);

            var responseBody = await response.Content.ReadAsStringAsync();

            var headers = response.Headers;

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(headers.TryGetValues("coco", out var cocoValues));
            Assert.Equal("lolo", cocoValues.First());
            Assert.True(headers.TryGetValues("zozo", out var zozoValues));
            Assert.Equal("zaza", zozoValues.First());

            Assert.Equal("cocoyes", responseBody);
        }

        [Fact]
        public async Task Validate_MockedResponse_With_Type()
        {
            // Arrange
            var yamlContent = """
                rules:
                - filter: 
                    typeKind: anyFilter  
                    operation: endsWith
                  action : 
                    typeKind: mockedResponseAction
                    response:
                      statusCode: 200
                      body:
                        type: json
                        text: >
                          { "coco" : false }
                """;

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"https://veryinvalidhost79795-sfsdfdsf.com/cookies");

            // Act 
            using var response = await Exec(yamlContent, requestMessage);

            var responseBody = await response.Content.ReadAsStringAsync();

            var headers = response.Content.Headers;

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(headers.TryGetValues("content-type", out var contentTypeValues));
            Assert.StartsWith("application/json", contentTypeValues.First());
        }

        [Fact]
        public async Task Validate_MockedResponse_With_Variable()
        {
            Environment.SetEnvironmentVariable("MyVar", "coco");

            // Arrange
            var yamlContent = """
                rules:
                - filter: 
                    typeKind: anyFilter  
                    operation: endsWith
                  action : 
                    typeKind: mockedResponseAction
                    response:
                      statusCode: 200
                      body:
                        type: text
                        text: >
                          ${env.MyVar}
                """;

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"https://veryinvalidhost79795-sfsdfdsf.com/cookies");

            // Act 
            using var response = await Exec(yamlContent, requestMessage);

            var responseBody = await response.Content.ReadAsStringAsync();

            var headers = response.Content.Headers;

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("coco", responseBody);
        }
    }
}
