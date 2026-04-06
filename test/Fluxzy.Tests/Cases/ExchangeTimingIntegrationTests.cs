// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.Cases
{
    public class ExchangeTimingIntegrationTests
    {
        [Fact]
        public async Task RequestBodySent_Should_Be_Earlier_Or_Equal_To_ResponseHeaderStart()
        {
            await using var proxy = new AddHocProxy(expectedRequestCount: 1, timeoutSeconds: 30);

            using var clientHandler = new HttpClientHandler {
                Proxy = new WebProxy($"http://{proxy.BindHost}:{proxy.BindPort}")
            };

            using var httpClient = new HttpClient(clientHandler);

            using var response = await httpClient.GetAsync("https://www.fluxzy.io/sitemap.xml");

            response.EnsureSuccessStatusCode();

            await proxy.WaitUntilDone();

            var exchange = proxy.CapturedExchanges.First();

            var requestBodySent = exchange.Metrics.RequestBodySent;
            var responseHeaderStart = exchange.Metrics.ResponseHeaderStart;

            Assert.True(requestBodySent <= responseHeaderStart,
                $"RequestBodySent ({requestBodySent:O}) should be earlier or equal to ResponseHeaderStart ({responseHeaderStart:O})");
        }
        [Fact]
        public async Task Post_With_Body_Should_Have_Zero_Or_Positive_Latency()
        {
            await using var proxy = new AddHocProxy(expectedRequestCount: 1, timeoutSeconds: 30);

            using var clientHandler = new HttpClientHandler {
                Proxy = new WebProxy($"http://{proxy.BindHost}:{proxy.BindPort}")
            };

            using var httpClient = new HttpClient(clientHandler);

            using var content = new StringContent("{\"data\":\"test\"}", Encoding.UTF8, "application/json");
            using var response = await httpClient.PostAsync("https://www.fluxzy.io/sitemap.xml", content);

            await proxy.WaitUntilDone();

            var exchange = proxy.CapturedExchanges.First();

            var requestBodySent = exchange.Metrics.RequestBodySent;
            var responseHeaderStart = exchange.Metrics.ResponseHeaderStart;
            var latency = responseHeaderStart - requestBodySent;

            Assert.True(latency >= TimeSpan.Zero,
                $"Latency should be zero or positive but was {latency.TotalMilliseconds}ms " +
                $"(RequestBodySent={requestBodySent:O}, ResponseHeaderStart={responseHeaderStart:O})");
        }
    }
}
