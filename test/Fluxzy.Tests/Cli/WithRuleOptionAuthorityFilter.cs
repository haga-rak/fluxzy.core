// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Fluxzy.Tests.Cli
{
    public class WithRuleOptionAuthorityFilter : WithRuleOptionBase
    {
        [Fact]
        public async Task Validate_AuthorityFilter()
        {
            // Arrange
            var yamlContent = $"""
                               rules:
                               - filter:
                                   typeKind: AuthorityFilter
                                   pattern: www.example.com
                                   port: 443
                                   operation: exact
                                 action :
                                   typeKind: addResponseHeaderAction
                                   headerName: Passed
                                   headerValue: true
                               """;

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"https://www.example.com/");

            // Act
            using var response = await Exec(yamlContent, requestMessage, allowAutoRedirect: false);
            await response.Content.ReadAsStringAsync();

            // Assert
            Assert.True(response.Headers.TryGetValues("Passed", out  _));
        }

        [Fact]
        public async Task Validate_AuthorityFilter_Not_Pass()
        {
            // Arrange
            var yamlContent = $"""
                               rules:
                               - filter:
                                   typeKind: AuthorityFilter
                                   pattern: www.exdmple.com
                                   port: 443
                                   operation: exact
                                 action :
                                   typeKind: addResponseHeaderAction
                                   headerName: Passed
                                   headerValue: true
                               """;

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"https://www.example.com/");

            // Act
            using var response = await Exec(yamlContent, requestMessage, allowAutoRedirect: false);
            await response.Content.ReadAsStringAsync();

            // Assert
            Assert.False(response.Headers.TryGetValues("Passed", out  _));
        }
    }
}
