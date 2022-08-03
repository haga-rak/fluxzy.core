using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Rules;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters.RequestFilters;
using Fluxzy.Tests.Tools;
using Fluxzy.Tests.Utils;
using Xunit;

namespace Fluxzy.Tests.Rules
{
    public class AlterationRules
    {
        [Theory]
        [InlineData(TestConstants.Http11Host)]
        [InlineData(TestConstants.Http2Host)]  
        public async Task AddNewRequestHeaderWithFilterHostOnly(string host)
        {
            var headerValue = "anyrandomtexTyoo!!";
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
                $"{host}/global-health-check");

            using var response = await httpClient.SendAsync(requestMessage);
            
            var checkResult = await response.GetCheckResult();
            
            var matchingHeaders = checkResult.
                Headers
                .Where(h => 
                    h.Name.Equals(headerName, StringComparison.OrdinalIgnoreCase) 
                    && h.Value == headerValue)
                .ToList();

            Assert.Single(matchingHeaders);

            await proxy.WaitUntilDone();
        }
        

        [Theory]
        [InlineData(TestConstants.Http11Host)]
        [InlineData(TestConstants.Http2Host)]  
        public async Task UpdateRequestHeaderWithFilterHostOnly(string host)
        {
            var headerName = "X-Haga-Unit-Test";
            var headerValue = "X-Haga-Unit-Test-value!!";
            var headerNewValue = "updated to ABCDef";

            using var proxy = new AddHocConfigurableProxy(PortProvider.Next(), 1, 10);

            using var clientHandler = new HttpClientHandler
            {
                Proxy = new WebProxy($"http://{proxy.BindHost}:{proxy.BindPort}"),
            };

            proxy.StartupSetting.AlterationRules.Add(
                new Rule(
                    new UpdateRequestHeaderAction(
                        headerName, headerNewValue),
                    new HostFilter("sandbox.smartizy.com")));

            proxy.Run();

            using var httpClient = new HttpClient(clientHandler);

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"{host}/global-health-check");

            requestMessage.Headers.Add(headerName, headerValue);

            using var response = await httpClient.SendAsync(requestMessage);
            
            var checkResult = await response.GetCheckResult();

            var matchingHeaders = checkResult.
                Headers?.Where(h => 
                    h.Name.Equals(headerName, StringComparison.OrdinalIgnoreCase) 
                    && h.Value == headerNewValue)
                .ToList();

            Assert.NotNull(matchingHeaders);
            Assert.Single(matchingHeaders);

            await proxy.WaitUntilDone();
        }

        [Theory]
        [InlineData(TestConstants.Http11Host)]
        [InlineData(TestConstants.Http2Host)]  
        public async Task UpdateRequestHeaderReuseExistingValueWithFilterHostOnly(string host)
        {
            var headerName = "x-h";
            var headerValue = "Cd";
            var headerNewValue = "{{previous}} Ab";
            var headerValueAltered = "Cd Ab"; 

            using var proxy = new AddHocConfigurableProxy(PortProvider.Next(), 1, 10);

            using var clientHandler = new HttpClientHandler
            {
                Proxy = new WebProxy($"http://{proxy.BindHost}:{proxy.BindPort}"),
            };

            proxy.StartupSetting.AlterationRules.Add(
                new Rule(
                    new UpdateRequestHeaderAction(
                        headerName, headerNewValue),
                    new HostFilter("sandbox.smartizy.com")));

            proxy.Run();

            using var httpClient = new HttpClient(clientHandler);

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"{host}/global-health-check");

            requestMessage.Headers.Add(headerName, headerValue);

            using var response = await httpClient.SendAsync(requestMessage);
            
            var checkResult = await response.GetCheckResult();

            var matchingHeaders = checkResult.
                Headers?.Where(h => 
                    h.Name.Equals(headerName, StringComparison.OrdinalIgnoreCase) 
                    && h.Value == headerValueAltered)
                .ToList();

            Assert.NotNull(matchingHeaders);
            Assert.Single(matchingHeaders);

            await proxy.WaitUntilDone();
        }
    }
}
