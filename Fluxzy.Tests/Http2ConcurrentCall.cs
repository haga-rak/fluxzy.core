// Copyright © 2021 Haga Rakotoharivelo

using System;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Fluxzy.Clients.DotNetBridge;
using Fluxzy.Tests.Common;
using Xunit;
using Header2 = fluxzy.sandbox.models.Header;

namespace Fluxzy.Tests
{
    public class Http2ConcurrentCall
    {
        public async Task CallSimple(
            HttpClient httpClient,
            int bufferSize, int length, NameValueCollection? nvCol = null)
        {
            var requestMessage = new HttpRequestMessage(
                HttpMethod.Post,
                $"{TestConstants.Http2Host}/global-health-check"
            );

            requestMessage.Headers.Add("x-buffer-size", bufferSize.ToString());

            if (nvCol != null)
                foreach (string nv in nvCol)
                    requestMessage.Headers.Add(nv, nvCol[nv]);

            await using var randomStream = new RandomDataStream(9, length);
            var content = new StreamContent(randomStream, bufferSize);

            requestMessage.Content = content;

            var response = await httpClient.SendAsync(requestMessage);

            Assert.True(response.IsSuccessStatusCode);

            await AssertionHelper.ValidateCheck(requestMessage, null, response);
        }

        [Fact]
        public async Task Post_Random_Data_And_Validate_Content()
        {
            using var handler = new FluxzyHttp2Handler();
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
        [Fact]
        public async Task Post_Multi_Header_Dynamic_Table_Evict_Simple()
        {
            using var handler = new FluxzyHttp2Handler();
            using var httpClient = new HttpClient(handler, false);

            var count = 20;

            var buffer = new byte[500];

            var tasks =
                Enumerable.Repeat(httpClient, count).Select((h, index) =>
                {
                    new Random(index).NextBytes(buffer);

                    return CallSimple(h, 1024 * 16 + 10, 128 * 4, new NameValueCollection
                    {
                        { "Cookie", Convert.ToBase64String(buffer) }
                    });
                });

            await Task.WhenAll(tasks);
        }

        /// <summary>
        ///     The goal of this test is to challenge the dynamic table content
        /// </summary>
        /// <returns></returns>
        [Theory]
        [InlineData(1)]
        [InlineData(16394)]
        public async Task Post_Dynamic_Table_Evict_Simple_Large_Object(int bodyLength)
        {
            using var handler = new FluxzyHttp2Handler();

            using var httpClient = new HttpClient(handler, false);

            var count = 1;

            var buffer = new byte[500];

            var tasks =
                Enumerable.Repeat(httpClient, count).Select((h, index) =>
                {
                    new Random(index % 2).NextBytes(buffer);

                    return CallSimple(
                        h, bodyLength,
                        524288, new NameValueCollection
                        {
                            { "Cookie", Convert.ToBase64String(buffer) }
                        });
                });

            await Task.WhenAll(tasks);
        }

        [Fact]
        public async Task Headers_Multiple_Reception()
        {
            using var handler = new FluxzyHttp2Handler();

            using var httpClient = new HttpClient(handler, false);

            var repeatCount = 20;

            var tasks = Enumerable.Repeat(httpClient, repeatCount)
                                  .Select(async client =>
                                  {
                                      var response = await client.GetAsync($"{TestConstants.Http2Host}/headers-random");
                                      var text = await response.Content.ReadAsStringAsync();

                                      var items = JsonSerializer.Deserialize<Header2[]>(text
                                          , new JsonSerializerOptions
                                          {
                                              PropertyNameCaseInsensitive = true
                                          })!;

                                      var mustBeTrue = items.All(i => response.Headers.Any(t => t.Key == i.Name
                                          && t.Value.Contains(i.Value)));

                                      Assert.True(mustBeTrue);
                                  });

            await Task.WhenAll(tasks);
        }

        //[Fact]
        public async Task Headers_Multiple_Reception_Repeating_Value()
        {
            await Task.WhenAll(Enumerable.Repeat(0, 10)
                                         .Select(p => Receiving_Multiple_Repeating_Header_Value_Call()));
        }

        private static async Task Receiving_Multiple_Repeating_Header_Value_Call()
        {
            using var handler = new FluxzyHttp2Handler();

            using var httpClient = new HttpClient(handler, false);

            var repeatCount = 40;

            var tasks = Enumerable.Repeat(httpClient, repeatCount)
                                  .Select(async client =>
                                  {
                                      var response =
                                          await client.GetAsync($"{TestConstants.Http2Host}/headers-random-repeat");

                                      var text = await response.Content.ReadAsStringAsync();

                                      var items = JsonSerializer.Deserialize<Header2[]>(text
                                          , new JsonSerializerOptions
                                          {
                                              PropertyNameCaseInsensitive = true
                                          })!;

                                      var mustBeTrue = items.All(i => response.Headers.Any(t => t.Key == i.Name
                                          && t.Value.Contains(i.Value)));

                                      Assert.True(mustBeTrue);
                                  });

            await Task.WhenAll(tasks);
        }
    }
}
