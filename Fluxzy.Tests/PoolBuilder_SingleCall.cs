using System.Net.Http;
using System.Threading.Tasks;
using Echoes.Clients.DotNetBridge;
using Xunit;

namespace Echoes.H2.Tests
{
    public class PoolBuilder_SingleCall
    {
        [Fact]
        public async Task Get_H2()
        {
            using var handler = new EchoesDefaultHandler();
            using var httpClient = new HttpClient(handler);

            HttpRequestMessage requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                "https://extranet.2befficient.fr/Scripts/Core?v=RG4zfPZTCmDTC0sCJZC1Fx9GEJ_Edk7FLfh_lQ"
            );

            var response = await httpClient.SendAsync(requestMessage);

            Assert.True(response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task Get_H1()
        {
            using var handler = new EchoesDefaultHandler();
            using var httpClient = new HttpClient(handler);

            HttpRequestMessage requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                "https://sandbox.smartizy.com/ip"
            );

            var response = await httpClient.SendAsync(requestMessage);

            Assert.True(response.IsSuccessStatusCode);
        }


        [Fact]
        public async Task Get_ConcurrentDemand()
        {
            var urls = new[]
            {
                "https://sandbox.smartizy.com:5001/content-produce/400000/400000", // H1.1 H2 url
                "https://sandbox.smartizy.com/content-produce/400000/400000" // H1 only url
            };

            using var handler = new EchoesDefaultHandler();
            using var httpClient = new HttpClient(handler);

            for (int i = 0; i < 100; i ++ )
            {
                HttpRequestMessage requestMessage = new HttpRequestMessage(
                    HttpMethod.Get,
                    urls[i%urls.Length]
                );

                using var response = await httpClient.SendAsync(requestMessage);

                Assert.True(response.IsSuccessStatusCode);
            }

        }

    }
}