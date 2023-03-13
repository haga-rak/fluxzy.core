// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Fluxzy.Core.Breakpoints;
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
        public async Task EndPointAndChangeToLocalHost(string host)
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

        [Theory]
        [InlineData(TestConstants.Http11Host)]
        [InlineData(TestConstants.Http2Host)]
        public async Task FilterSkip(string host)
        {
            await using var proxy = new AddHocConfigurableProxy(1, 10);

            proxy.StartupSetting.AlterationRules.Add(
                new Rule(
                    new BreakPointAction(),
                    new HostFilter("sandbox.smartizyerror.com")
                ));

            var endPoint = proxy.Run().First();

            using var clientHandler = new HttpClientHandler {
                Proxy = new WebProxy($"http://{endPoint}")
            };

            using var httpClient = new HttpClient(clientHandler);

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"{host}/global-health-check");

            var responseTask = httpClient.SendAsync(requestMessage);

            var response = await responseTask;

            Assert.Equal(200, (int) response.StatusCode);

            await proxy.WaitUntilDone();
        }

        [Theory]
        [InlineData(TestConstants.Http11Host)]
        [InlineData(TestConstants.Http2Host)]
        public async Task RequestBreakAndChange(string host)
        {
            await using var proxy = new AddHocConfigurableProxy(1, 10);

            var newRequestHeader =
                "GET /gloglo.txt HTTP/1.1\r\n" +
                "host: sandbox.smartizy.com\r\n" +
                "x-header-added: value\r\n" +
                "\r\n";

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

            if (!EditableRequestHeaderSet.TryParse(newRequestHeader, 
                    Array.Empty<byte>(), 
                    out var headerSet).Success) {
                Assert.Fail("Fail to parse request header, check your test arrangement"); 
            }
            
            context.RequestHeaderCompletion.SetValue(headerSet!.ToRequest());

            context.ContinueAll();

            var response = await responseTask;

            var actualBodyString = await response.Content.ReadAsStringAsync();

            Assert.Equal(404, (int) response.StatusCode);

            await proxy.WaitUntilDone();
        }

        [Theory]
        [InlineData(TestConstants.Http11Host, "")]
        [InlineData(TestConstants.Http2Host, "")]
        [InlineData(TestConstants.Http11Host, "abcedef")]
        [InlineData(TestConstants.Http2Host, "abcedef")]
        public async Task ResponseBreakAndChange(string host, string payloadString)
        {
            await using var proxy = new AddHocConfigurableProxy(1, 10);

            var payload = Encoding.UTF8.GetBytes(payloadString); 

            var fullResponseHeader =
                "HTTP/1.1 203 OkBuddy\r\n" +
                $"Content-length: 900\r\n" +
                "x-header-added: value\r\n" +
                "\r\n";

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

            if (!EditableResponseHeaderSet.TryParse(fullResponseHeader,
                    payload, 
                    out var headerSet).Success) {
                Assert.Fail("Fail to parse response header, check your test arrangement"); 
            }
            
            context.ResponseHeaderCompletion.SetValue(headerSet!.ToResponse());

            context.ContinueAll();

            var response = await responseTask;

            var actualBodyString = await response.Content.ReadAsStringAsync();

            Assert.Equal(203, (int) response.StatusCode);
            Assert.Equal(payloadString, actualBodyString);

            await proxy.WaitUntilDone();
        }
    }
}
