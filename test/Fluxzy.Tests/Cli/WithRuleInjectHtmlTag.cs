// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Fluxzy.Tests.Cli
{
    public class WithRuleInjectHtmlTag : WithRuleOptionBase
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Validate_Inject_Flat_Text(bool useCompression)
        {
            // Arrange
            var yamlContent = """
                               rules:
                               - filter:
                                   typeKind: anyFilter
                                 action :
                                   typeKind: InjectHtmlTagAction
                                   tag: head
                                   text: '<style>body { background-color: red !important; }</style>'
                               """;

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"https://example.com");

            // Act
            using var response = await Exec(yamlContent, requestMessage, automaticDecompression: useCompression);

            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.True(responseBody.Contains("<style>body { background-color:", StringComparison.Ordinal));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Validate_Inject_From_File(bool useCompression)
        {
            // Arrange
            var yamlContent = """
                               rules:
                               - filter:
                                   typeKind: anyFilter
                                 action :
                                   typeKind: InjectHtmlTagAction
                                   tag: head
                                   fromFile: true
                                   fileName: _Files/Rules/Injected/injected-script.js
                               """;

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"https://example.com");

            // Act
            using var response = await Exec(yamlContent, requestMessage, automaticDecompression: useCompression);

            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.True(responseBody.Contains("injected-after-head-tag", StringComparison.Ordinal));
        }
    }
}
