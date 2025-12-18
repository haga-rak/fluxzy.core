// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Rules.Filters;
using Xunit;

namespace Fluxzy.Tests.Cli
{
    public class WithRuleOptionQueryStringFilter : WithRuleOptionBase
    {
        [Theory]
        [InlineData("id", "123456", StringSelectorOperation.Exact, "/path?id=123456", true)]
        [InlineData("", "123456", StringSelectorOperation.Exact, "/path?id=123456", true)]
        [InlineData("id", "123456", StringSelectorOperation.Exact, "/path?id=123459", false)]
        [InlineData("", "123456", StringSelectorOperation.Exact, "/path?id=123459", false)]
        [InlineData("id", "123 456", StringSelectorOperation.Exact, "/path?id=123%20456", true)]
        [InlineData("id", "\\d+", StringSelectorOperation.Regex, "/path?id=123456", true)]
        [InlineData("id", "^\\d+$", StringSelectorOperation.Regex, "/path?id=123456", true)]
        [InlineData("id", "\\d+", StringSelectorOperation.Regex, "/path?id=123a56", true)]
        [InlineData("id", "^\\d+$", StringSelectorOperation.Regex, "/path?id=123a56", false)]
        public async Task Validate_QueryStringFilter(string name, string pattern,
            StringSelectorOperation stringSelectorOperation, string pathAndQuery, bool pass)
        {
            // Arrange
            var yamlContent = $"""
                               rules:
                               - filter:
                                   typeKind: queryStringFilter
                                   name: "{name}"
                                   pattern: '{pattern}'
                                   operation: {stringSelectorOperation.ToString()}
                                 action :
                                   typeKind: addResponseHeaderAction
                                   headerName: Passed
                                   headerValue: true
                               """;

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"https://sandbox.fluxzy.io{pathAndQuery}");

            // Act
            using var response = await Exec(yamlContent, requestMessage, allowAutoRedirect: false);
            await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(pass, response.Headers.TryGetValues("Passed", out _));
        }
    }
}
