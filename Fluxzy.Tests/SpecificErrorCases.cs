// Copyright © 2022 Haga Rakotoharivelo

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Tests.Tools;
using Fluxzy.Tests.Utils;
using Xunit;

namespace Fluxzy.Tests
{
    public class SpecificErrorCases
    {
        [Theory]
        [InlineData(TestConstants.Http2Host)]
        public async Task Get_Gfe_Nvidia_Com(string host)
        {
            await using var proxy = new AddHocProxy(1, 10);

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