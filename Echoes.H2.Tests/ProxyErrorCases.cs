// Copyright © 2022 Haga Rakotoharivelo

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Echoes.H2.Tests.Tools;
using Echoes.H2.Tests.Utils;
using Xunit;

namespace Echoes.H2.Tests
{
    public class ProxyErrorCases
    {
        [Theory]
        [InlineData(TestConstants.Http2Host)]
        public async Task Get_Gfe_Nvidia_Com(string host)
        {
            using var proxy = new AddHocProxy(PortProvider.Next(), 1, 10);

            using var clientHandler = new HttpClientHandler
            {
                Proxy = new WebProxy($"http://{proxy.BindHost}:{proxy.BindPort}"),
            };

            using var httpClient = new HttpClient(clientHandler);

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"https://gql.twitch.tv/gql"
                //$"https://wcpstatic.microsoft.com/mscc/lib/v2/wcp-consent.js"
                
                );

            using var response = await httpClient.SendAsync(requestMessage);

            var responesString = await response.Content.ReadAsStringAsync();

           // Assert.True(response.IsSuccessStatusCode);


            await proxy.WaitUntilDone();
        }
    }
}