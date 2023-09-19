// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Fluxzy.Tests.Cli
{
    public class WithRuleOptionNoOpAction : WithRuleOptionBase
    {
        [Fact]
        public async Task Validate_Simple()
        {
            // Arrange
            var yamlContent = $"""
                               rules:
                               - filter:
                                   typeKind: AnyFilter
                                 action :
                                   typeKind: noOpAction
                               """;

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"https://www.example.com/");

            // Act
            using var response = await Exec(yamlContent, requestMessage, allowAutoRedirect: false);
            Assert.True(response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task Validate_Simple_2()
        {
            // Arrange
            var yamlContent = $"""
                               rules:
                               - filter:
                                   typeKind: AnyFilter
                                 actions:
                                   - typeKind: noOpAction
                               """;

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"https://www.example.com/");

            // Act
            using var response = await Exec(yamlContent, requestMessage, allowAutoRedirect: false);
            Assert.True(response.IsSuccessStatusCode);
        }
    }
}
