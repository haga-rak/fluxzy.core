// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.Cases
{
    public class ChunkedRequestToH2UpstreamTests
    {
        [Fact]
        public async Task Chunked_Request_Body_Is_Forwarded_Over_H2()
        {
            await using var proxy = new AddHocConfigurableProxy(1, 10);

            using var httpClient = proxy.RunAndGetClient();

            await using var randomStream = new RandomDataStream(9, 1024 * 124);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"https://{TestConstants.HttpBinHost}/post") {
                Content = new StreamContent(randomStream, 8192)
            };

            using var response = await httpClient.SendAsync(requestMessage);
            var contentText = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            AssertHelpers
                .ControlHeaders(contentText, requestMessage)
                .ControlBody(randomStream.Hash);

            await proxy.WaitUntilDone();

            Assert.Equal("HTTP/2", proxy.CapturedExchanges.Single().HttpVersion);
        }
    }
}