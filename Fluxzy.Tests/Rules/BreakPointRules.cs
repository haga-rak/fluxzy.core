// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Rules;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters.RequestFilters;
using Fluxzy.Tests.Common;
using Xunit;

namespace Fluxzy.Tests.Rules
{
    public class BreakPointRules
    {
        [Theory]
        [InlineData(TestConstants.Http11Host)]
        [InlineData(TestConstants.Http2Host)]
        public async Task BreakOnEndPointAndChangeToLocalHost(string host)
        {
            await using var proxy = new AddHocConfigurableProxy(1, 10);

            proxy.StartupSetting.AlterationRules.Add(
                new Rule(
                     new BreakPointAction(),
                     new HostFilter("sandbox.smartizy.com")
                ));

            var endPoint = proxy.Run().First();

            using var clientHandler = new HttpClientHandler {
                Proxy = new WebProxy($"http://{endPoint}")
            };

            using var httpClient = new HttpClient(clientHandler);

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"{host}/global-health-check");

            var responseTask = httpClient.SendAsync(requestMessage);

            var context = await proxy.InternalProxy.ExecutionContext.BreakPointManager.ContextQueue.ReadAsync();

            context.EndPointCompletion.SetValue(new IPEndPoint(IPAddress.Loopback, 852));

            context.ContinueAll();

            var response = await responseTask;

            var actualBodyString = await response.Content.ReadAsStringAsync();

            Assert.Equal(528, (int) response.StatusCode);

            await proxy.WaitUntilDone();
        }
    }
}
