using System;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Echoes.DotNetBridge;
using Echoes.H2.Tests.Utils;
using Xunit;

namespace Echoes.H2.Tests
{
    
    public class Http11_ConcurrentCall
    {
        public async Task CallSimple(
            HttpClient httpClient, 
            int bufferSize, int length, NameValueCollection? nvCol = null)
        {
            HttpRequestMessage requestMessage = new HttpRequestMessage(
                HttpMethod.Post,
                "https://httpbin.org/post"
            );

            requestMessage.Headers.Add("x-buffer-size", length.ToString());

            if (nvCol != null)
            {
                foreach (string nv in nvCol)
                {
                    requestMessage.Headers.Add(nv, nvCol[nv]);
                }
            }
            
            await using var randomStream = new RandomDataStream(9, length, true);

            requestMessage.Content = new StreamContent(randomStream);


            using var response = await httpClient.SendAsync(requestMessage);
             var contentText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            Assert.True(response.IsSuccessStatusCode);

            AssertHelpers.ControlHeaders(contentText, requestMessage, length); 
        }

        [Fact]
        public async Task Post_Random_Data_And_Validate_Content()
        {
            using var handler = new EchoesHttp11Handler();
            using var httpClient = new HttpClient(handler, false);

            Random random = new Random(9);

            int count = 15;

            var tasks = 
                Enumerable.Repeat(httpClient, count).Select(h =>
                    CallSimple(h, (1024 * 16) + 10, 1024 * 4));

            await Task.WhenAll(tasks); 
        }

        /// <summary>
        /// The goal of this test is to challenge the dynamic table content
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Post_Multi_Header_Dynamic_Table_Evict_Simple()
        {
            using var handler = new EchoesHttp11Handler();

            using var httpClient = new HttpClient(handler, false);

            int count = 150;

            byte[] buffer = new byte[500]; 

            var tasks = 
                Enumerable.Repeat(httpClient, count).Select((h, index) =>
                {
                    new Random(index%2).NextBytes(buffer);

                    return CallSimple(h, (1024 * 16) + 10, 512, new NameValueCollection()
                    {
                        { "Cookie" , Convert.ToBase64String(buffer) }
                    });
                });

            await Task.WhenAll(tasks); 
        }
    }
}