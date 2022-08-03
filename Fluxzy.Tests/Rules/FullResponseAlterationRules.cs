// Copyright © 2022 Haga Rakotoharivelo

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Clients.Mock;
using Fluxzy.Rules;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters.RequestFilters;
using Fluxzy.Tests.Tools;
using Fluxzy.Tests.Utils;
using Xunit;

namespace Fluxzy.Tests.Rules
{
    public class FullResponseAlterationRules
    {
        [Theory]
        [InlineData(TestConstants.Http11Host)]
        [InlineData(TestConstants.Http2Host)]
        public async Task AddNewResponseHeaderWithFilterHostOnly(string host)
        {
            var bodyString = "This will be the default body you received"; 

            using var proxy = new AddHocConfigurableProxy(PortProvider.Next(), 1, 10);

            using var clientHandler = new HttpClientHandler
            {
                Proxy = new WebProxy($"http://{proxy.BindHost}:{proxy.BindPort}"),
            };

            proxy.StartupSetting.AlterationRules.Add(
                new Rule(
                    new FullResponseAction(new ReplyStreamContent(403, 
                        Clients.Mock.BodyContent.CreateFromString(bodyString))),
                    new HostFilter("sandbox.smartizy.com")
                    ));

            proxy.Run();

            using var httpClient = new HttpClient(clientHandler);

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"{host}/global-health-check");

            using var response = await httpClient.SendAsync(requestMessage);

            var actualBodyString = await response.Content.ReadAsStringAsync();
            
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal(bodyString, actualBodyString);

            await proxy.WaitUntilDone();
        }
    }
}