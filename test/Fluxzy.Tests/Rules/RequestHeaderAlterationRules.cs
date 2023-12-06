// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Rules;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters.RequestFilters;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.Rules
{
    public class RequestHeaderAlterationRules
    {
        [Theory]
        [InlineData(TestConstants.Http11Host)]
        [InlineData(TestConstants.Http2Host)]
        public async Task AddNewRequestHeaderWithFilterHostOnly(string host)
        {
            var headerValue = "anyrandomtexTyoo!!";
            var headerName = "X-Haga-Unit-Test";

            await using var proxy = new AddHocConfigurableProxy(1, 10);

            proxy.StartupSetting.AddAlterationRules(
                new Rule(
                    new AddRequestHeaderAction(
                        headerName, headerValue),
                    new HostFilter("sandbox.smartizy.com")));

            var endPoint = proxy.Run().First();

            using var clientHandler = new HttpClientHandler {
                Proxy = new WebProxy($"http://{endPoint}")
            };

            using var httpClient = new HttpClient(clientHandler);

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"{host}/global-health-check");

            using var response = await httpClient.SendAsync(requestMessage);

            var checkResult = await response.GetCheckResult();

            var matchingHeaders =
                checkResult.Headers?.Where(h =>
                               h.Name.Equals(headerName, StringComparison.OrdinalIgnoreCase)
                               && h.Value == headerValue)
                           .ToList();

            Assert.NotNull(matchingHeaders);
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

            await using var proxy = new AddHocConfigurableProxy(1, 10);

            proxy.StartupSetting.AddAlterationRules(
                new Rule(
                    new UpdateRequestHeaderAction(
                        headerName, headerNewValue),
                    new HostFilter("sandbox.smartizy.com")));

            var endPoint = proxy.Run().First();

            using var clientHandler = new HttpClientHandler {
                Proxy = new WebProxy($"http://{endPoint}")
            };

            using var httpClient = new HttpClient(clientHandler);

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"{host}/global-health-check");

            requestMessage.Headers.Add(headerName, headerValue);

            using var response = await httpClient.SendAsync(requestMessage);

            var checkResult = await response.GetCheckResult();

            var matchingHeaders = checkResult.Headers?.Where(h =>
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
        public async Task UpdateRequestHeaderWithFilterHostOnlyIfMissing(string host)
        {
            var headerName = "X-Haga-Unit-Test";
            var headerNewValue = "updated to ABCDef";

            await using var proxy = new AddHocConfigurableProxy(1, 10);

            proxy.StartupSetting.AddAlterationRules(
                new Rule(
                    new UpdateRequestHeaderAction(
                        headerName, headerNewValue) {
                        AddIfMissing = true
                    },
                    new HostFilter("sandbox.smartizy.com")));

            var endPoint = proxy.Run().First();

            using var clientHandler = new HttpClientHandler {
                Proxy = new WebProxy($"http://{endPoint}")
            };

            using var httpClient = new HttpClient(clientHandler);

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"{host}/global-health-check");
            
            using var response = await httpClient.SendAsync(requestMessage);

            var checkResult = await response.GetCheckResult();

            var matchingHeaders = checkResult.Headers?.Where(h =>
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

            await using var proxy = new AddHocConfigurableProxy(1, 10);

            proxy.StartupSetting.AddAlterationRules(
                new Rule(
                    new UpdateRequestHeaderAction(
                        headerName, headerNewValue),
                    new HostFilter("sandbox.smartizy.com")));

            var endPoint = proxy.Run().First();

            using var clientHandler = new HttpClientHandler {
                Proxy = new WebProxy($"http://{endPoint}")
            };

            using var httpClient = new HttpClient(clientHandler);

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"{host}/global-health-check");

            requestMessage.Headers.Add(headerName, headerValue);

            using var response = await httpClient.SendAsync(requestMessage);

            var checkResult = await response.GetCheckResult();

            var matchingHeaders = checkResult.Headers?.Where(h =>
                                                 h.Name.Equals(headerName, StringComparison.OrdinalIgnoreCase)
                                                 && h.Value == headerValueAltered)
                                             .ToList();

            Assert.NotNull(matchingHeaders);
            Assert.Single(matchingHeaders);

            await proxy.WaitUntilDone();
        }

        [Theory]
        [InlineData(TestConstants.Http11Host)]
        [InlineData(TestConstants.Http2Host)]
        public async Task DeleteRequestHeaderWithFilterHostOnly(string host)
        {
            var headerName = "X-Haga-Unit-Test";

            await using var proxy = new AddHocConfigurableProxy(1, 10);

            proxy.StartupSetting.AddAlterationRules(
                new Rule(
                    new DeleteRequestHeaderAction(headerName),
                    new HostFilter("sandbox.smartizy.com")));

            var endPoint = proxy.Run().First();

            using var clientHandler = new HttpClientHandler {
                Proxy = new WebProxy($"http://{endPoint}")
            };

            using var httpClient = new HttpClient(clientHandler);

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"{host}/global-health-check");

            using var response = await httpClient.SendAsync(requestMessage);

            var checkResult = await response.GetCheckResult();

            var matchingHeaders = checkResult.Headers?
                                             .Where(h =>
                                                 h.Name.Equals(headerName, StringComparison.OrdinalIgnoreCase))
                                             .ToList();

            Assert.NotNull(matchingHeaders);
            Assert.Empty(matchingHeaders);

            await proxy.WaitUntilDone();
        }

        [Theory]
        [InlineData(TestConstants.Http11Host)]
        [InlineData(TestConstants.Http2Host)]
        public async Task ChangeMethodFilterHostOnly(string host)
        {
            await using var proxy = new AddHocConfigurableProxy(1, 10);


            proxy.StartupSetting.AddAlterationRules(
                new Rule(
                    new ChangeRequestMethodAction("PATCH"),
                    new HostFilter("sandbox.smartizy.com")));

            var endPoint = proxy.Run().First();

            using var clientHandler = new HttpClientHandler {
                Proxy = new WebProxy($"http://{endPoint}")
            };

            using var httpClient = new HttpClient(clientHandler);

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"{host}/global-health-check");

            using var response = await httpClient.SendAsync(requestMessage);

            var checkResult = await response.GetCheckResult();

            Assert.Equal("PATCH", checkResult.Method, StringComparer.OrdinalIgnoreCase);

            await proxy.WaitUntilDone();
        }
    }
}
