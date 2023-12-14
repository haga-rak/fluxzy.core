// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Fluxzy.Tests.Cli
{
    public class WithRuleInjectAfterHtmlTag : WithRuleOptionBase
    {
        [Fact]
        public async Task ValidateInject()
        {
            // Arrange
            var yamlContent = """
                               rules:
                               - filter:
                                   typeKind: anyFilter
                                 action :
                                   typeKind: InjectAfterHtmlTagAction
                                   tag: head
                                   text: '<style>body { background-color: red !important; }</style>'
                               """;

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"https://example.com");

            // Act

            using var response = await Exec(yamlContent, requestMessage);

            var responseBody = await response.Content.ReadAsStringAsync();

            Assert.True(responseBody.Contains("<style>body { background-color:", StringComparison.Ordinal));
        }

        [Fact]
        public async Task ValidateInject_CompressedBody()
        {
            // Arrange
            var yamlContent = """
                               rules:
                               - filter:
                                   typeKind: anyFilter
                                 action :
                                   typeKind: InjectAfterHtmlTagAction
                                   tag: head
                                   text: '<style>body { background-color: red !important; }</style>'
                               """;

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"https://example.com");

            // Act

            using var response = await Exec(yamlContent, requestMessage, automaticDecompression: true);

            var responseBody = await response.Content.ReadAsStringAsync();

            Assert.True(responseBody.Contains("<style>body { background-color:", StringComparison.Ordinal));
        }
    }
}
