// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Actions.HighLevelActions;
using Fluxzy.Tests._Fixtures;
using Microsoft.VisualBasic;
using Xunit;

namespace Fluxzy.Tests.Cli
{
    public class WithRuleUpStreamProxyAction : WithRuleOptionBase
    {
        [Fact]
        public async Task Validate_Mocked()
        {
            // Create a second proxy to chain
            var fluxzySetting = FluxzySetting.CreateLocalRandomPort();
            var content = "Mocked by secondary proxy";

            fluxzySetting.ConfigureRule()
                         .WhenAny()
                         .ReplyText(content, 402);

            await using var proxy = new Proxy(fluxzySetting);
            var endPoint = proxy.Run().First();

            // Arrange
            var yamlContent = $"""
                               rules:
                               - filter:
                                   typeKind: AnyFilter
                                 actions :     
                                 - typeKind: SkipRemoteCertificateValidationAction
                                 - typeKind: UpStreamProxyAction
                                   host: {endPoint.Address}
                                   port: {endPoint.Port}
                               """;

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                TestConstants.TestDomain);

            // Act
            using var response = await Exec(yamlContent, requestMessage, allowAutoRedirect: false);

            var fullResponseString = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.PaymentRequired, response.StatusCode);
            Assert.Equal(content, fullResponseString);
        }
        
        [Theory]
        [CombinatorialData]
        public async Task Validate_Pass_Through(
            [CombinatorialValues(
                TestConstants.Http11Host,
                TestConstants.Http2Host, 
                TestConstants.PlainHttp11)] string host,
            [CombinatorialValues(null, "leelo:multipass")]
                string ? auth)
        {
            // Arrange

            // Create a second proxy to chain
            var fluxzySetting = FluxzySetting.CreateLocalRandomPort();
            string proxyHeaderValue = ""; 

            if (auth != null) {
                var tab = auth.Split(':');
                var login = tab[0];
                var password = tab[1];

                proxyHeaderValue = $"proxyAuthorizationHeader: Basic {Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{login}:{password}"))}";

                fluxzySetting.SetProxyAuthentication(ProxyAuthentication.Basic(login, password));
            }

            fluxzySetting.ConfigureRule()
                         .WhenAny()
                         .AddResponseHeader("X-Test-Result", "Secondary Proxy");

            await using var proxy = new Proxy(fluxzySetting);
            var endPoint = proxy.Run().First();
            
            var yamlContent = $"""
                               rules:
                               - filter:
                                   typeKind: AnyFilter
                                 actions :     
                                 - typeKind: SkipRemoteCertificateValidationAction
                                 - typeKind: UpStreamProxyAction
                                   host: {endPoint.Address}
                                   port: {endPoint.Port}
                                   {proxyHeaderValue}
                               """;

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"{host}/global-health-check");

            // Act
            using var response = await Exec(yamlContent, requestMessage, allowAutoRedirect: false);

            response.Headers.TryGetValues("X-Test-Result", out var values);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Secondary Proxy", values?.First());
        }
    }
}
