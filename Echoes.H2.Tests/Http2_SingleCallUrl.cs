using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Echoes.DotNetBridge;
using Echoes.H2.Tests.Utils;
using Xunit;

namespace Echoes.H2.Tests
{
    public class Http2_SingleCallUrl
    {
        [Fact]
        public async Task Get_IIS()
        {
            using var handler = new EchoesHttp2Handler();
            using var httpClient = new HttpClient(handler); 

            HttpRequestMessage requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                "https://extranet.2befficient.fr/Scripts/Core?v=RG4zfPZTCmDTC0sCJZC1Fx9GEJ_Edk7FLfh_lQ"
            );

            var response = await httpClient.SendAsync(requestMessage);

            Assert.True(response.IsSuccessStatusCode);
        }


        [Fact]
        public async Task Get_Error_Case()
        {
            using var handler = new EchoesHttp2Handler();
            using var httpClient = new HttpClient(handler);

            HttpRequestMessage requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                "https://fr.wiktionary.org/w/skins/Vector/resources/common/images/arrow-down.svg?9426f"
            );

            var response = await httpClient.SendAsync(requestMessage);

            Assert.True(response.IsSuccessStatusCode);
        }
        [Fact]
        public async Task Get_Error_Case_Discord()
        {
            using var handler = new EchoesHttp2Handler();
            using var httpClient = new HttpClient(handler);

            HttpRequestMessage requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                "https://discord.com/assets/afe2828ad8a44f9ed87d.js"
            );

            var response = await httpClient.SendAsync(requestMessage);

            Assert.True(response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task Get_Error_Case_Ws_Static()
        {
            using var handler = new EchoesHttp2Handler();
            using var httpClient = new HttpClient(handler);

            HttpRequestMessage requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                "https://wcpstatic.microsoft.com/mscc/lib/v2/wcp-consent.js"
            );

            var response = await httpClient.SendAsync(requestMessage);
            var responseData = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode);
        }


        [Fact]
        public async Task Get_Control_Single_Headers()
        {
            using var handler = new EchoesHttp2Handler();
            using var httpClient = new HttpClient(handler); 

            HttpRequestMessage requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                "https://httpbin.org/get"
            );
            
            var response = await httpClient.SendAsync(requestMessage);
            var contentText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            Assert.True(response.IsSuccessStatusCode);
            AssertHelpers.ControlHeaders(contentText, requestMessage);
        }

        [Fact]
        public async Task Get_With_200_Simple()
        {
            using var handler = new EchoesHttp2Handler();
            using var httpClient = new HttpClient(handler); 

            HttpRequestMessage requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                "https://httpbin.org/ip"
            );
            
            var response = await httpClient.SendAsync(requestMessage);
            var contentText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            Assert.True(response.IsSuccessStatusCode);
            AssertHelpers.ControlHeaders(contentText, requestMessage);
        }

        [Fact]
        public async Task Get_With_204_No_Body()
        {
            using var handler = new EchoesHttp2Handler();
            using var httpClient = new HttpClient(handler); 

            HttpRequestMessage requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                $"{TestConstants.Http2Host}/content-produce/0/0"
            );
            
            var response = await httpClient.SendAsync(requestMessage);
            var contentText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(contentText == string.Empty);
        }

        [Fact]
        public async Task Get_Control_Duplicate_Headers()
        {
            using var handler = new EchoesHttp2Handler();
            using var httpClient = new HttpClient(handler); 

            HttpRequestMessage requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                "https://httpbin.org/get"
            );

            requestMessage.Headers.Add("x-favorite-header", "1");
            requestMessage.Headers.Add("X-fAVorite-header", "2");

            var response = await httpClient.SendAsync(requestMessage);
            var contentText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);


            Assert.True(response.IsSuccessStatusCode);
            AssertHelpers.ControlHeaders(contentText, requestMessage);
        }

        [Fact]
        public async Task Post_Data_Lt_Max_Frame()
        {
            using var handler = new EchoesHttp2Handler();
            using var httpClient = new HttpClient(handler); 

            HttpRequestMessage requestMessage = new HttpRequestMessage(
                HttpMethod.Post,
                "https://httpbin.org/post"
            );

            var bufferString = new string('a', (16 * 1024) - 9);

            var content = new StringContent(bufferString, System.Text.Encoding.UTF8);
            content.Headers.ContentLength = content.Headers.ContentLength;

            requestMessage.Content = content;
            
            var response = await httpClient.SendAsync(requestMessage);
            var contentText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);


            Assert.True(response.IsSuccessStatusCode);
            AssertHelpers.ControlHeaders(contentText, requestMessage);
        }

        [Fact]
        public async Task Post_Data_Gt_Max_Frame()
        {
            using var handler = new EchoesHttp2Handler();
            using var httpClient = new HttpClient(handler); 

            HttpRequestMessage requestMessage = new HttpRequestMessage(
                HttpMethod.Post,
                "https://httpbin.org/post"
            );
            
            var bufferString = new string('a', (16 * 1024) + 10);

            var content = new StringContent(bufferString, System.Text.Encoding.UTF8);
            content.Headers.ContentLength = content.Headers.ContentLength;

            requestMessage.Content = content;
            
            requestMessage.ToHttp11String();
            
            var response = await httpClient.SendAsync(requestMessage);
            var contentText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            
            Assert.True(response.IsSuccessStatusCode);
            AssertHelpers.ControlHeaders(contentText, requestMessage);
        }

        [Fact]
        public async Task Post_Data_Unknown_Size()
        {
            using var handler = new EchoesHttp2Handler();
            using var httpClient = new HttpClient(handler); 

            HttpRequestMessage requestMessage = new HttpRequestMessage(
                HttpMethod.Post,
                "https://httpbin.org/post"
            );
            
            using var randomStream = new RandomDataStream(9, 1024 * 124);
            var content = new StreamContent(randomStream, 8192);

            requestMessage.Content = content;
            
            var response = await httpClient.SendAsync(requestMessage);
            var contentText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            
            Assert.True(response.IsSuccessStatusCode);

            AssertHelpers
                .ControlHeaders(contentText, requestMessage)
                .ControlBody(randomStream.Hash);
        }


        [Fact]
        public async Task Get_With_InvalidHeaders()
        {
            using var handler = new EchoesHttp2Handler();
            using var httpClient = new HttpClient(handler);

            HttpRequestMessage requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                "https://httpbin.org/get"
            );

            requestMessage.Headers.Add("Connection", "Keep-alive" );

            var response = await httpClient.SendAsync(requestMessage);
            var contentText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            Assert.True(response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task Get_With_Extra_Column_Header()
        {
            using var handler = new EchoesHttp2Handler();
            using var httpClient = new HttpClient(handler);

            HttpRequestMessage requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                "https://httpbin.org/get"
            );

            requestMessage.Headers.Add("x-Header-a", "ads");
            
            var response = await httpClient.SendAsync(requestMessage);

            var contentText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            Assert.True(response.IsSuccessStatusCode);
            AssertHelpers.ControlHeaders(contentText, requestMessage);
        }

        [Fact]
        public async Task Get_And_Cancel()
        {
            using var handler = new EchoesHttp2Handler();
            using var httpClient = new HttpClient(handler);

            HttpRequestMessage requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                "https://httpbin.org/get"
            );

            CancellationTokenSource source = new CancellationTokenSource();

            await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            {
                var responsePromise = httpClient.SendAsync(requestMessage, source.Token);
                source.Cancel();
                await responsePromise;
            });
        }
        

    }
}