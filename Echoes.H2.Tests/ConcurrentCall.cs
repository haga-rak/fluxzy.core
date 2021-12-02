// Copyright © 2021 Haga Rakotoharivelo

using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Echoes.H2.DotNetBridge;
using Xunit;

namespace Echoes.H2.Tests
{
    public class ConcurrentCall
    {
        public async Task CallSimple(HttpClient httpClient, int bufferSize, Random random)
        {
            HttpRequestMessage requestMessage = new HttpRequestMessage(
                HttpMethod.Post,
                "https://httpbin.org/post"
            );

            await using var randomStream = new RandomDataStream(9, 1024 * 18);
            var content = new StreamContent(randomStream, 8192);

            requestMessage.Content = content;

            requestMessage.Headers.Add("x-buffer-size", bufferSize.ToString());

            var response = await httpClient.SendAsync(requestMessage);
            var contentText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            Assert.True(response.IsSuccessStatusCode);

            AssertHelpers.ControlHeaders(contentText, requestMessage, bufferSize)
                .ControlBody(randomStream.Hash);
        }

        [Fact]
        public async Task Post_Random_Data()
        {
            using var handler = new EchoesHttp2Handler();
            using var httpClient = new HttpClient(handler, false);

            Random random = new Random(9);

            int count = 40;

            var tasks = 
                Enumerable.Repeat(httpClient, count).Select(h =>
                CallSimple(h, (1024 * 16) + 10, random));

            await Task.WhenAll(tasks); 

        }
    }
}