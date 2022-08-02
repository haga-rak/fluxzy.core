using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Fluxzy.Rules;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters.RequestFilters;
using Fluxzy.Tests.Tools;
using Fluxzy.Tests.Utils;
using Xunit;

namespace Fluxzy.Tests.Rules
{
    public class SimpleRules
    {
        [Theory]
        [InlineData(TestConstants.Http11Host)]
        // [InlineData(TestConstants.Http2Host)]
        public async Task CheckAlterationAddRuleWithFilterHostOnly(string host)
        {
            var headerValue = "anyrandomtextyoo!!";
            var headerName = "X-Haga-Unit-Test";

            using var proxy = new AddHocConfigurableProxy(PortProvider.Next(), 1, 10);

            using var clientHandler = new HttpClientHandler
            {
                Proxy = new WebProxy($"http://{proxy.BindHost}:{proxy.BindPort}"),
            };

            proxy.StartupSetting.AlterationRules.Add(
                new Rule(
                    new AddRequestHeaderAction(
                        headerName, headerValue),
                    new HostFilter("sandbox.smartizy.com")));

            proxy.Run();

            using var httpClient = new HttpClient(clientHandler);

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"{host}/global-health-check?aaaa5=789");

            using var response = await httpClient.SendAsync(requestMessage);

            var responseRawText = await response.Content.ReadAsStringAsync(); 
            var checkResult = await response.GetCheckResult();

            var matchingHeaders = checkResult.
                Headers.Where(h => h.Name == headerName && h.Value == headerValue)
                .ToList(); ;

            Assert.Single(matchingHeaders);

            await proxy.WaitUntilDone();
        }
    }
}
