// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Certificates;
using Xunit;

namespace Fluxzy.Tests.Cli
{
    public class WitRuleOptionServerCertificateAction : WithRuleOptionBase
    {
        public async Task Validate()
        {
            CertificateBuilder 

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
    }
}
