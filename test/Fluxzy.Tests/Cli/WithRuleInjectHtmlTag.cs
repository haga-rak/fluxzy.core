// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.Cli
{
    public class WithRuleInjectHtmlTag : WithRuleOptionBase
    {
        [Theory]
        [CombinatorialData]
        public async Task Validate_Inject_Flat_Text(
            [CombinatorialValues(true, false)]
            bool useCompression,
            [CombinatorialValues(true, false)]
            bool useHttp11
            )
        {
            var testUrl = $"{TestConstants.TestDomain}";
            
            var action = useHttp11 ? "ForceHttp11Action" : "NoOpAction";

            // Arrange
            var yamlContent = $$$"""
                                 rules:
                                 - filter:
                                     typeKind: anyFilter
                                   actions :
                                     - typeKind: InjectHtmlTagAction
                                       tag: head
                                       htmlContent: '<style>body { background-color: red !important; }</style>'
                                     - typeKind: {{{action}}}
                                 """;

            for (int i = 0; i < 2; i++) {

                var requestMessage = new HttpRequestMessage(HttpMethod.Get, testUrl);

                // Act
                using var response = await Exec(yamlContent, requestMessage, automaticDecompression: useCompression);

                var responseBody = await response.Content.ReadAsStringAsync();

                // Assert
                Assert.True(responseBody.Contains("<style>body { background-color:", StringComparison.Ordinal));
            }

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

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, TestConstants.TestDomainPage);

            // Act
            using var response = await Exec(yamlContent, requestMessage, automaticDecompression: useCompression);

            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.True(responseBody.Contains("injected-after-head-tag", StringComparison.Ordinal));
        }
    }
}
