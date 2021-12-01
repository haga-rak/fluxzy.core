using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Echoes.H2.DotNetBridge;
using Xunit;

namespace Echoes.H2.Tests
{
    public class SingleCallUrl
    {
        [Fact]
        public async Task Simple_Call()
        {
            using var handler = new EchoesHttp2Handler();
            using var httpClient = new HttpClient(handler); 

            HttpRequestMessage requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                "https://extranet.2befficient.fr/Scripts/Core?v=RG4zfPZTCmDTC0sCJZC1Fx9GEJ_Edk7FLfh_lQ"
            );

            var response = await httpClient.SendAsync(requestMessage);



        }
    }
}