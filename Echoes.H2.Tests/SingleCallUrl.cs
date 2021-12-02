using System.Net.Http;
using System.Threading.Tasks;
using Echoes.H2.DotNetBridge;
using Xunit;

namespace Echoes.H2.Tests
{
    public class SingleCallUrl
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

            var str = requestMessage.ToHttp11String();
            
            var response = await httpClient.SendAsync(requestMessage);
            var contentText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);


            Assert.True(response.IsSuccessStatusCode);
            AssertHelpers.ControlHeaders(contentText, requestMessage);
        }
    }
}