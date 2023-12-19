// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests
{
    public class ReverseProxyTests
    {
        [Theory]
        [InlineData(TestConstants.Http11Host)]
        [InlineData(TestConstants.Http2Host)]
        public async Task Secure(string fullHost)
        {
            var finalUrl = $"{fullHost}/global-health-check";
            var uri = new Uri(finalUrl);
            var host = uri.Host;
            var repetition = 10; 

            await using var proxy = new AddHocProxy(1, 10, configureSetting: setting => {
                setting.SetReverseMode(true);
                setting.SetReverseModeForcedPort(uri.Port);
            });

            var proxyPort = proxy.BindPort;

            var handler = ReverseProxyHelper.GetSpoofedHandler(proxyPort, host);

            using var httpClient = new HttpClient(handler, false);
            
            var requestBodyLength = 23632;

            for (int i = 0; i < repetition; i++)
            {
                var requestMessage = new HttpRequestMessage(HttpMethod.Post, uri);

                await using var randomStream = new RandomDataStream(i, requestBodyLength, true);
                await using var hashedStream = new HashedStream(randomStream);

                requestMessage.Content = new StreamContent(hashedStream);
                requestMessage.Headers.Add("X-Identifier", "Simple header");

                using var response = await httpClient.SendAsync(requestMessage);

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                await AssertionHelper.ValidateCheck(requestMessage, hashedStream.Hash, response);
            }
        }

        [Theory]
        [InlineData(TestConstants.PlainHttp11)]
        public async Task Plain(string fullHost)
        {
            var finalUrl = $"{fullHost}/global-health-check";
            var uri = new Uri(finalUrl);
            var host = uri.Host;
            var repetition = 5; 

            await using var proxy = new AddHocProxy(1, 10, configureSetting: setting => {
                setting.SetReverseMode(true);
                setting.SetReverseModeForcedPort(uri.Port);
                setting.SetReverseModePlainHttp(true);
            });

            var proxyPort = proxy.BindPort;

            var handler = ReverseProxyHelper.GetSpoofedHandler(proxyPort, host, secure: false);

            using var httpClient = new HttpClient(handler, false);
            
            var requestBodyLength = 23632;

            for (int i = 0; i < repetition; i++)
            {
                var requestMessage = new HttpRequestMessage(HttpMethod.Post, uri);

                await using var randomStream = new RandomDataStream(i, requestBodyLength, true);
                await using var hashedStream = new HashedStream(randomStream);

                requestMessage.Content = new StreamContent(hashedStream);
                requestMessage.Headers.Add("X-Identifier", "Simple header");

                using var response = await httpClient.SendAsync(requestMessage);

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                await AssertionHelper.ValidateCheck(requestMessage, hashedStream.Hash, response);
            }
        }

        [Theory]
        [InlineData(TestConstants.PlainHttp11)]
        public async Task Plain_No_Body(string fullHost)
        {
            var finalUrl = $"{fullHost}/global-health-check";
            var uri = new Uri(finalUrl);
            var host = uri.Host;
            var repetition = 5; 

            await using var proxy = new AddHocProxy(1, 10, configureSetting: setting => {
                setting.SetReverseMode(true);
                setting.SetReverseModeForcedPort(uri.Port);
                setting.SetReverseModePlainHttp(true);
            });

            var proxyPort = proxy.BindPort;

            var handler = ReverseProxyHelper.GetSpoofedHandler(proxyPort, host, secure: false);

            using var httpClient = new HttpClient(handler, false);
            
            for (int i = 0; i < repetition; i++)
            {
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
                requestMessage.Headers.Add("X-Identifier", "Simple header");

                using var response = await httpClient.SendAsync(requestMessage);

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                await AssertionHelper.ValidateCheck(requestMessage, null, response);
            }
        }
 }
}
