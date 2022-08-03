// Copyright © 2022 Haga Rakotoharivelo

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
    public class ResponseHeaderAlterationRules
    {
        [Theory]
        [InlineData(TestConstants.Http11Host)]
        [InlineData(TestConstants.Http2Host)]
        public async Task AddNewResponseHeaderWithFilterHostOnly(string host)
        {
            var headerValue = "value!!!";
            var headerName = "X-pRepend-Header";

            using var proxy = new AddHocConfigurableProxy(PortProvider.Next(), 1, 10);

            using var clientHandler = new HttpClientHandler
            {
                Proxy = new WebProxy($"http://{proxy.BindHost}:{proxy.BindPort}"),
            };

            proxy.StartupSetting.AlterationRules.Add(
                new Rule(
                    new AddResponseHeaderAction(
                        headerName, headerValue),
                    new HostFilter("sandbox.smartizy.com")));

            proxy.Run();

            using var httpClient = new HttpClient(clientHandler);

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"{host}/global-health-check");

            using var response = await httpClient.SendAsync(requestMessage);

            var checkResult = await response.GetCheckResult();

            var headerIsPresent =
                response.Headers.TryGetValues(headerName, out var actualResponseHeaders); 

            Assert.True(headerIsPresent);
            Assert.NotNull(actualResponseHeaders);
            Assert.Single(actualResponseHeaders);
            Assert.Equal(headerValue, actualResponseHeaders.First());

            await proxy.WaitUntilDone();
        }

        [Theory]
        [InlineData(TestConstants.Http11Host)]
        [InlineData(TestConstants.Http2Host)]
        public async Task UpdateResponseHeaderWithFilterHostOnly(string host)
        {
            var headerValue = "fluxzy-proxy";
            var headerName = "Server";

            using var proxy = new AddHocConfigurableProxy(PortProvider.Next(), 1, 10);

            using var clientHandler = new HttpClientHandler
            {
                Proxy = new WebProxy($"http://{proxy.BindHost}:{proxy.BindPort}"),
            };

            proxy.StartupSetting.AlterationRules.Add(
                new Rule(
                    new UpdateResponseHeaderAction(
                        headerName, headerValue),
                    new HostFilter("sandbox.smartizy.com")));

            proxy.Run();

            using var httpClient = new HttpClient(clientHandler);

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"{host}/global-health-check");

            using var response = await httpClient.SendAsync(requestMessage);
            
           var headerIsPresent =
                response.Headers.TryGetValues(headerName, out var actualResponseHeaders); 

            Assert.True(headerIsPresent);
            Assert.NotNull(actualResponseHeaders);
            Assert.Single(actualResponseHeaders);
            Assert.Equal(headerValue, actualResponseHeaders.First());

            await proxy.WaitUntilDone();
        }

        [Theory]
        [InlineData(TestConstants.Http11Host)]
        [InlineData(TestConstants.Http2Host)]
        public async Task UpdateResponseHeaderReuseExistingValueWithFilterHostOnly(string host)
        {
            var headerValue = "{{previous}}fluxzy-proxy";
            var headerName = "Server";

            using var proxy = new AddHocConfigurableProxy(PortProvider.Next(), 1, 10);

            using var clientHandler = new HttpClientHandler
            {
                Proxy = new WebProxy($"http://{proxy.BindHost}:{proxy.BindPort}"),
            };

            proxy.StartupSetting.AlterationRules.Add(
                new Rule(
                    new UpdateResponseHeaderAction(
                        headerName, headerValue),
                    new HostFilter("sandbox.smartizy.com")));

            proxy.Run();

            using var httpClient = new HttpClient(clientHandler);

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"{host}/global-health-check");

            using var response = await httpClient.SendAsync(requestMessage);
            
           var headerIsPresent =
                response.Headers.TryGetValues(headerName, out var actualResponseHeaders); 

            Assert.True(headerIsPresent);
            Assert.NotNull(actualResponseHeaders);
            Assert.Single(actualResponseHeaders);
            Assert.EndsWith("fluxzy-proxy", actualResponseHeaders.First());

            await proxy.WaitUntilDone();
        }


        [Theory]
        [InlineData(TestConstants.Http11Host)]
        [InlineData(TestConstants.Http2Host)]
        public async Task DeleteResponseHeaderWithFilterHostOnly(string host)
        {
            var headerName = "server";

            using var proxy = new AddHocConfigurableProxy(PortProvider.Next(), 1, 10);

            using var clientHandler = new HttpClientHandler
            {
                Proxy = new WebProxy($"http://{proxy.BindHost}:{proxy.BindPort}"),
            };

            proxy.StartupSetting.AlterationRules.Add(
                new Rule(
                    new DeleteResponseHeaderAction(
                        headerName),
                    new HostFilter("sandbox.smartizy.com")));

            proxy.Run();

            using var httpClient = new HttpClient(clientHandler);

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"{host}/global-health-check");

            using var response = await httpClient.SendAsync(requestMessage);

            var checkResult = await response.GetCheckResult();

            var headerIsPresent =
                response.Headers.TryGetValues(headerName, out var actualResponseHeaders);

            Assert.False(headerIsPresent);

            await proxy.WaitUntilDone();
        }
    }
}