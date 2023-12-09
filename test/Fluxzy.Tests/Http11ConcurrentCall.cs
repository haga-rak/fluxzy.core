// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Clients.DotNetBridge;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests
{
    public class Http11ConcurrentCall
    {
        private async Task CallSimple(
            HttpClient httpClient,
            int anotherBufferSize, int length, NameValueCollection? nvCol = null)
        {
            var requestMessage = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://{TestConstants.HttpBinHost}/post"
            );

            requestMessage.Headers.Add("x-buffer-size", length.ToString());

            if (nvCol != null) {
                foreach (string nv in nvCol) {
                    requestMessage.Headers.Add(nv, nvCol[nv]);
                }
            }

            await using var randomStream = new RandomDataStream(9, length, true);

            requestMessage.Content = new StreamContent(randomStream);

            using var response = await httpClient.SendAsync(requestMessage);
            var contentText = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode, response.ToString());
            AssertHelpers.ControlHeaders(contentText, requestMessage, length);
        }

        [Theory]
        [InlineData(SslProvider.BouncyCastle)]
        [InlineData(SslProvider.OsDefault)]
        public async Task Post_Random_Data_And_Validate_Content(SslProvider sslProvider)
        {
            using var handler = new FluxzyHttp11Handler(sslProvider);
            using var httpClient = new HttpClient(handler, false);

            var random = new Random(9);

            var count = 15;

            var tasks =
                Enumerable.Repeat(httpClient, count).Select(h =>
                    CallSimple(h, 1024 * 16 + 10, 1024 * 4));

            await Task.WhenAll(tasks);
        }

        /// <summary>
        ///     The goal of this test is to challenge the dynamic table content
        /// </summary>
        /// <returns></returns>
        [Theory]
        [InlineData(SslProvider.BouncyCastle)]
        [InlineData(SslProvider.OsDefault)]
        public async Task Post_Multi_Header_Dynamic_Table_Evict_Simple(SslProvider sslProvider)
        {
            using var handler = new FluxzyHttp11Handler(sslProvider);

            using var httpClient = new HttpClient(handler, false);

            var count = 150;

            var buffer = new byte[500];

            var tasks =
                Enumerable.Repeat(httpClient, count).Select((h, index) => {
                    new Random(index % 2).NextBytes(buffer);

                    return CallSimple(h, 1024 * 16 + 10, 512, new NameValueCollection {
                        { "Cookie", Convert.ToBase64String(buffer) }
                    });
                });

            await Task.WhenAll(tasks);
        }
    }
}
