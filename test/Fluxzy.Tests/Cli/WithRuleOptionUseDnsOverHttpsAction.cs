// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Fluxzy.Tests.Cli
{
    public class WithRuleOptionUseDnsOverHttpsAction : WithRuleOptionBase
    {
        [Theory]
        [CombinatorialData]
        public async Task Validate_Simple_Request(
            [CombinatorialValues("GOOGLE", "CLOUDFLARE", "https://dns.google.com/resolve")] string nameOrUrl,
            [CombinatorialValues("https://www.example.com", "https://microsoft.com/", "http://1.1.1.1")]
            string url,
            [CombinatorialValues(false, true)]
            bool noCapture)
        {
            // Arrange
            var yamlContent = $"""
                               rules:
                               - filter:
                                   typeKind: anyFilter
                                 action :
                                   typeKind: useDnsOverHttpsAction
                                   nameOrUrl: {nameOrUrl}
                                   noCapture: {noCapture}
                               """;

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);

            // Act 
            using var response = await Exec(yamlContent, requestMessage);

            if ((int) response.StatusCode == 528) {
                var fullStringResponse = await response.Content.ReadAsStringAsync();

                throw new Exception($"Error: {fullStringResponse}");
            }


            // Assert
            Assert.NotEqual(528, (int) response.StatusCode);
        }
    }
}
