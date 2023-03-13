// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Clients.Mock;
using Fluxzy.Rules;
using Fluxzy.Rules.Actions.HighLevelActions;
using Fluxzy.Rules.Filters.RequestFilters;
using Fluxzy.Tests.Common;
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

            await using var proxy = new AddHocConfigurableProxy(1, 10);

            proxy.StartupSetting.AlterationRules.Add(
                new Rule(
                    new FullResponseAction(new ReplyStreamContent(403,
                        Clients.Mock.BodyContent.CreateFromString(bodyString))),
                    new HostFilter("sandbox.smartizy.com")
                ));

            var endPoint = proxy.Run().First();

            using var clientHandler = new HttpClientHandler {
                Proxy = new WebProxy($"http://{endPoint}")
            };

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
