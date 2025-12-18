// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.Cli
{
    public class WithRuleOptionSetVariableAction : WithRuleOptionBase
    {
        [Fact]
        public async Task Validate_SetVariableAction()
        {
            // Arrange
            var yamlContent = """
                               rules:
                               - filter:
                                   typeKind: HostFilter
                                   pattern: 'sandbox.(?<FOO>[a-z]+).io'
                                   operation: regex
                                 actions :
                                   - typeKind: setVariableAction
                                     name: FOO
                                     value: '${user.FOO}BAR'
                                     scope: RequestHeaderReceivedFromClient
                                   - typeKind: addResponseHeaderAction
                                     headerName: Passed
                                     headerValue: '${user.FOO}'
                               """;

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                TestConstants.TestDomain);

            // Act
            using var response = await Exec(yamlContent, requestMessage, allowAutoRedirect: false);
            await response.Content.ReadAsStringAsync();

            // Assert
            Assert.True(response.Headers.TryGetValues("Passed", out var headerValue));
            Assert.Equal("fluxzyBAR", headerValue.First().ToString());
        }

    }
}
