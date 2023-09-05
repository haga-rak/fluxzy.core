// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Tests._Fixtures;
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
                $"https://{TestConstants.HttpBinHost}/cookies");

            // Act 
            using var response = await Exec(yamlContent, requestMessage);

            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal("cocoyes", responseBody);
        }
    }
}
