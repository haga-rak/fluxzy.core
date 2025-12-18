// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Core.Breakpoints;
using Fluxzy.Rules;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters.RequestFilters;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Rules
{
    public class BreakPointRules
    {
        public static IEnumerable<object[]> GetResponseBreakAndChangeParams
        {
            get
            {
                var hosts = new[] { TestConstants.Http11Host, TestConstants.Http2Host, TestConstants.PlainHttp11 };

                var breakpointPayloadTypes =
                    (TestBreakpointPayloadType[])Enum.GetValues(typeof(TestBreakpointPayloadType));

                foreach (var host in hosts)
                    foreach (var withPcap in breakpointPayloadTypes)
                    {
                        yield return new object[] { host, withPcap };
                    }
            }
        }

        public static IEnumerable<object[]> GetRequestBreakAndChangeParams
        {
            get
            {
                var hosts = new[] { TestConstants.Http11Host, TestConstants.Http2Host, TestConstants.PlainHttp11 };

                var breakpointPayloadTypes =
                    (TestBreakpointPayloadType[])Enum.GetValues(typeof(TestBreakpointPayloadType));

                foreach (var host in hosts)
                    foreach (var withPcap in breakpointPayloadTypes)
                    {
                        yield return new object[] { host, withPcap };
                    }
            }
        }

        [Theory]
        [MemberData(nameof(TestConstants.GetHosts), MemberType = typeof(TestConstants))]
        public async Task ContinueUntilEnd(string host)
        {
            await using var proxy = new AddHocConfigurableProxy(1, 10);

            proxy.StartupSetting.AddAlterationRules(
                new Rule(
                    new BreakPointAction(),
                    new HostFilter("sandbox.fluxzy.io")
                ));

            var endPoint = proxy.Run().First();

            using var clientHandler = new HttpClientHandler
            {
                Proxy = new WebProxy($"http://{endPoint}")
            };

            using var httpClient = new HttpClient(clientHandler);

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"{host}/global-health-check");

            var completionSourceContext = new TaskCompletionSource<BreakPointContext>();

            proxy.InternalProxy.ExecutionContext.BreakPointManager.OnContextUpdated += (sender, args) =>
            {
                completionSourceContext.TrySetResult(args.Context);
            };

            var responseTask = httpClient.SendAsync(requestMessage);

            var context = await completionSourceContext.Task;

            context.ContinueUntilEnd();

            var response = await responseTask;

            //var response = await httpClient.SendAsync(requestMessage);

            var _ = await response.Content.ReadAsStringAsync();

            Assert.Equal(200, (int)response.StatusCode);

            await proxy.WaitUntilDone();
        }

        [Theory]
        [MemberData(nameof(TestConstants.GetHosts), MemberType = typeof(TestConstants))]
        public async Task FilterSkip(string host)
        {
            await using var proxy = new AddHocConfigurableProxy(1, 10);

            proxy.StartupSetting.AddAlterationRules(
                new Rule(
                    new BreakPointAction(),
                    new HostFilter("sandbox.smartizyerror.com")
                ));

            var endPoint = proxy.Run().First();

            using var clientHandler = new HttpClientHandler
            {
                Proxy = new WebProxy($"http://{endPoint}")
            };

            using var httpClient = new HttpClient(clientHandler);

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"{host}/global-health-check");

            var responseTask = httpClient.SendAsync(requestMessage);

            var response = await responseTask;

            Assert.Equal(200, (int)response.StatusCode);

            await proxy.WaitUntilDone();
        }

        [Theory]
        [MemberData(nameof(TestConstants.GetHosts), MemberType = typeof(TestConstants))]
        public async Task ChangeDnsMapping(string host)
        {
            await using var proxy = new AddHocConfigurableProxy(1, 10);

            proxy.StartupSetting.AddAlterationRules(
                new Rule(
                    new BreakPointAction(),
                    new HostFilter("sandbox.fluxzy.io")
                ));

            var endPoint = proxy.Run().First();

            using var clientHandler = new HttpClientHandler
            {
                Proxy = new WebProxy($"http://{endPoint}")
            };

            using var httpClient = new HttpClient(clientHandler);

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"{host}/global-health-check");

            var completionSourceContext = new TaskCompletionSource<BreakPointContext>();

            proxy.InternalProxy.ExecutionContext.BreakPointManager.OnContextUpdated += (sender, args) =>
            {
                completionSourceContext.TrySetResult(args.Context);
            };

            var responseTask = httpClient.SendAsync(requestMessage);

            var context = await completionSourceContext.Task;

            context.ConnectionSetupCompletion.SetValue(new ConnectionSetupStepModel
            {
                IpAddress = IPAddress.Loopback.ToString(),
                Port = 523
            });

            context.ContinueUntilEnd();

            var response = await responseTask;

            //var response = await httpClient.SendAsync(requestMessage);

            var _ = await response.Content.ReadAsStringAsync();

            Assert.Equal(528, (int)response.StatusCode);

            await proxy.WaitUntilDone();
        }

        [Theory]
        [MemberData(nameof(GetRequestBreakAndChangeParams))]
        public async Task ChangeEntireRequest(string host, TestBreakpointPayloadType payloadType)
        {
            await using var proxy = new AddHocConfigurableProxy(1, 10);

            var stepModel = payloadType.GetRequestStepModel(out var payloadLength);

            var newRequestHeader =
                "GET /global-health-check HTTP/1.1\r\n" +
                "host: sandbox.fluxzy.io\r\n" +
                "x-header-added: value\r\n" +
                "\r\n";

            stepModel.FlatHeader = newRequestHeader;

            proxy.StartupSetting.AddAlterationRules(
                new Rule(
                    new BreakPointAction(),
                    new HostFilter("sandbox.fluxzy.io")
                ));

            var endPoint = proxy.Run().First();

            using var clientHandler = new HttpClientHandler
            {
                Proxy = new WebProxy($"http://{endPoint}")
            };

            using var httpClient = new HttpClient(clientHandler);

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"{host}/gloglo.text");

            var completionSourceContext = new TaskCompletionSource<BreakPointContext>();

            proxy.InternalProxy.ExecutionContext.BreakPointManager.OnContextUpdated += (sender, args) =>
            {
                completionSourceContext.TrySetResult(args.Context);
            };

            var responseTask = httpClient.SendAsync(requestMessage);

            var context = await completionSourceContext.Task;

            context.RequestHeaderCompletion.SetValue(stepModel);

            context.ContinueUntilEnd();

            var response = await responseTask;

            var checkResult = await response.GetCheckResult();

            Assert.Equal(200, (int)response.StatusCode);
            Assert.Equal(payloadLength, checkResult.RequestContent.Length ?? 0);

            await proxy.WaitUntilDone();
        }

        [Theory]
        [MemberData(nameof(TestConstants.GetHosts), MemberType = typeof(TestConstants))]
        public async Task ChangeEntireRequestInvalidHeaders(string host)
        {
            await using var proxy = new AddHocConfigurableProxy(1, 10);

            var payloadType = TestBreakpointPayloadType.FromString;

            var stepModel = payloadType.GetRequestStepModel(out var payloadLength);

            var newRequestHeader =
                "GET /global-health-check HTTP/1.1\r\n" +
                "host: sandbox.fluxzy.io\r\n" +
                "Connection: close\r\n" +
                "Transfer-encoding: chunked\r\n" +
                "x-header-added: value\r\n" +
                "\r\n";

            stepModel.FlatHeader = newRequestHeader;

            proxy.StartupSetting.AddAlterationRules(
                new Rule(
                    new BreakPointAction(),
                    new HostFilter("sandbox.fluxzy.io")
                ));

            var endPoint = proxy.Run().First();

            using var clientHandler = new HttpClientHandler
            {
                Proxy = new WebProxy($"http://{endPoint}")
            };

            using var httpClient = new HttpClient(clientHandler);

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"{host}/gloglo.text");

            var completionSourceContext = new TaskCompletionSource<BreakPointContext>();

            proxy.InternalProxy.ExecutionContext.BreakPointManager.OnContextUpdated += (sender, args) =>
            {
                completionSourceContext.TrySetResult(args.Context);
            };

            var responseTask = httpClient.SendAsync(requestMessage);

            var context = await completionSourceContext.Task;

            context.RequestHeaderCompletion.SetValue(stepModel);

            context.ContinueUntilEnd();

            var response = await responseTask;

            var checkResult = await response.GetCheckResult();

            Assert.Equal(200, (int)response.StatusCode);
            Assert.Equal(payloadLength, checkResult.RequestContent.Length);

            await proxy.WaitUntilDone();
        }

        [Theory]
        [MemberData(nameof(GetResponseBreakAndChangeParams))]
        public async Task ChangeEntireResponse(string host, TestBreakpointPayloadType payloadType)
        {
            await using var proxy = new AddHocConfigurableProxy(1, 20);

            var stepModel = payloadType.GetResponseStepModel(out var payloadLength);

            var fullResponseHeader =
                "HTTP/1.1 203 OkBuddy\r\n" +
                "Content-length: 900\r\n" +
                "x-header-added: value\r\n" +
                "\r\n";

            stepModel.FlatHeader = fullResponseHeader;

            proxy.StartupSetting.AddAlterationRules(
                new Rule(
                    new BreakPointAction(),
                    new HostFilter("sandbox.fluxzy.io")
                ));

            var endPoint = proxy.Run().First();

            using var clientHandler = new HttpClientHandler
            {
                Proxy = new WebProxy($"http://{endPoint}")
            };

            using var httpClient = new HttpClient(clientHandler);

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"{host}/global-health-check");

            var completionSourceContext = new TaskCompletionSource<BreakPointContext>();

            proxy.InternalProxy.ExecutionContext.BreakPointManager.OnContextUpdated += (sender, args) =>
            {
                completionSourceContext.TrySetResult(args.Context);
            };

            var responseTask = httpClient.SendAsync(requestMessage);

            var context = await completionSourceContext.Task;

            context.ResponseHeaderCompletion.SetValue(stepModel);

            context.ContinueUntilEnd();

            var response = await responseTask;

            var actualBodyString = await response.Content.ReadAsStringAsync();

            Assert.Equal(203, (int)response.StatusCode);
            Assert.Equal(payloadLength, actualBodyString.Length);
            Assert.Contains(response.Headers, t => t.Key.Equals("x-header-added"));

            await proxy.WaitUntilDone();
        }

        [Theory]
        [MemberData(nameof(TestConstants.GetHosts), MemberType = typeof(TestConstants))]
        public async Task ChangeEntireResponseInvalidHeaders(string host)
        {
            await using var proxy = new AddHocConfigurableProxy(1, 40);

            var payloadType = TestBreakpointPayloadType.FromFile;

            var stepModel = payloadType.GetResponseStepModel(out var payloadLength);

            var fullResponseHeader =
                "HTTP/1.1 203 OkBuddy\r\n" +
                "Content-length: 900\r\n" +
                "Connection: close\r\n" +
                "Transfer-encoding: chunked\r\n" +
                "x-header-added: value\r\n" +
                "\r\n";

            stepModel.FlatHeader = fullResponseHeader;

            proxy.StartupSetting.AddAlterationRules(
                new Rule(
                    new BreakPointAction(),
                    new HostFilter("sandbox.fluxzy.io")
                ));

            var endPoint = proxy.Run().First();

            using var clientHandler = new HttpClientHandler
            {
                Proxy = new WebProxy($"http://{endPoint}")
            };

            using var httpClient = new HttpClient(clientHandler);

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"{host}/global-health-check");

            var completionSourceContext = new TaskCompletionSource<BreakPointContext>();

            proxy.InternalProxy.ExecutionContext.BreakPointManager.OnContextUpdated += (sender, args) =>
            {
                completionSourceContext.TrySetResult(args.Context);
            };

            var responseTask = httpClient.SendAsync(requestMessage);

            var context = await completionSourceContext.Task;

            context.ResponseHeaderCompletion.SetValue(stepModel);

            context.ContinueUntilEnd();

            var response = await responseTask;

            var actualBodyString = await response.Content.ReadAsStringAsync();

            Assert.Equal(203, (int)response.StatusCode);
            Assert.Equal(payloadLength, actualBodyString.Length);
            Assert.Contains(response.Headers, t => t.Key.Equals("x-header-added"));

            await proxy.WaitUntilDone();
        }
    }

    public enum TestBreakpointPayloadType
    {
        NoPayload,
        FromString,
        FromFile
    }

    public static class TestBreakPointPayloadExtensions
    {
        public static ResponseSetupStepModel GetResponseStepModel(
            this TestBreakpointPayloadType type, out int payloadLength)
        {
            var fileName = Guid.NewGuid() + ".temp";

            switch (type)
            {
                case TestBreakpointPayloadType.NoPayload:
                    payloadLength = 0;

                    return new ResponseSetupStepModel
                    {
                        ContentBody = string.Empty,
                        FromFile = false
                    };

                case TestBreakpointPayloadType.FromFile:
                    File.WriteAllText(fileName, "FromFile");
                    payloadLength = "FromFile".Length;

                    return new ResponseSetupStepModel
                    {
                        ContentBody = "FromFile",
                        FromFile = true,
                        FileName = fileName
                    };

                case TestBreakpointPayloadType.FromString:
                    payloadLength = "FromString".Length;

                    return new ResponseSetupStepModel
                    {
                        ContentBody = "FromString",
                        FromFile = false
                    };

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        public static RequestSetupStepModel GetRequestStepModel(
            this TestBreakpointPayloadType type, out int payloadLength)
        {
            var fileName = Guid.NewGuid() + ".temp";

            switch (type)
            {
                case TestBreakpointPayloadType.NoPayload:
                    payloadLength = 0;

                    return new RequestSetupStepModel
                    {
                        ContentBody = string.Empty,
                        FromFile = false
                    };

                case TestBreakpointPayloadType.FromFile:
                    File.WriteAllText(fileName, "FromFile");
                    payloadLength = "FromFile".Length;

                    return new RequestSetupStepModel
                    {
                        ContentBody = "FromFile",
                        FromFile = true,
                        FileName = fileName
                    };

                case TestBreakpointPayloadType.FromString:
                    payloadLength = "FromString".Length;

                    return new RequestSetupStepModel
                    {
                        ContentBody = "FromString",
                        FromFile = false
                    };

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }
    }
}
