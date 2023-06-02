// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Tests._Fixtures;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Fluxzy.Tests.Cli
{
    public class FluxzyAsWebServer
    {
        [Theory]
        [InlineData("127.0.0.1")]
        [InlineData("localhost")]
        public async Task Should_Retrieve_Ca_File(string host)
        {
            await using var proxyInstance  = await RequestHelper.WaitForSingleRequest();

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"http://{host}:{proxyInstance.ListenPort}/ca");

            var httpClient = new HttpClient();

            var response = await httpClient.SendAsync(requestMessage);
            var responseArray = await response.Content.ReadAsByteArrayAsync();

            Assert.True(response.IsSuccessStatusCode);
            Assert.Equal(2220, responseArray.Length);
        }

        [Theory]
        [InlineData("127.0.0.1")]
        [InlineData("localhost")]
        public async Task Should_Retrieve_Main_Page(string host)
        {
            await using var proxyInstance  = await RequestHelper.WaitForSingleRequest();

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"http://{host}:{proxyInstance.ListenPort}");

            var httpClient = new HttpClient();

            var response = await httpClient.SendAsync(requestMessage);
            var responseString = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode);
        }
    }
}
