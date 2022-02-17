// Copyright © 2022 Haga Rakotoharivelo

using System.ComponentModel.DataAnnotations.Schema;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Echoes.H2.Tests;
using Echoes.H2.Tests.Tools;
using Echoes.H2.Tests.Utils;
using Xunit;

namespace Echoes.Tests
{
    public class ErrorCases
    {
        [Theory]
        [InlineData(TestConstants.Http2Host)]
        public async Task Connection_Close_Before_Response(string host)
        {
            using var proxy = new AddHocProxy(PortProvider.Next());

            using var clientHandler = new HttpClientHandler
            {
                Proxy = new WebProxy($"http://{proxy.BindHost}:{proxy.BindPort}"),
            };

            using var httpClient = new HttpClient(clientHandler);

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"{host}/connection-broken-abort-before-response");
            
            using var response = await httpClient.SendAsync(requestMessage);

            var responseBody = await response.Content.ReadAsStringAsync(); 

            Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
            Assert.True(!string.IsNullOrWhiteSpace(responseBody));
        }

        //TESTER SUR Sandbox UN ENVOI D ENTETE MULTIPLE POUR FORCER LE DYNAMIC TableAttribute UPDATE
        //    [Theory]
        [InlineData(TestConstants.Http2Host)]
        public async Task LargeHeaderFieldValue(string host)
        {
            using var proxy = new AddHocProxy(PortProvider.Next());

            using var clientHandler = new HttpClientHandler
            {
                Proxy = new WebProxy($"http://{proxy.BindHost}:{proxy.BindPort}"),
            };

            using var httpClient = new HttpClient(clientHandler);

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"{host}/ip");

            requestMessage.Headers.Add("x-lar-value", new string('v', 4096));
            requestMessage.Headers.Add("x-lar-value", new string('z', 4096));


            using var response = await httpClient.SendAsync(requestMessage);

            var responseBody = await response.Content.ReadAsStringAsync(); 
            
            Assert.True(!string.IsNullOrWhiteSpace(responseBody));
        }

        [Fact]
        public async Task Connection_RefusedTcplevel()
        {
            using var proxy = new AddHocProxy(PortProvider.Next());

            using var clientHandler = new HttpClientHandler
            {
                Proxy = new WebProxy($"http://{proxy.BindHost}:{proxy.BindPort}"),
            };

            using var httpClient = new HttpClient(clientHandler);

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"https://sandbox.smartizy.com:4988/");
            
            using var response = await httpClient.SendAsync(requestMessage);

            var responseBody = await response.Content.ReadAsStringAsync(); 

            Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
            Assert.True(!string.IsNullOrWhiteSpace(responseBody));
        }
        
    }
}