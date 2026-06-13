// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Tests._Fixtures;
using Fluxzy.Utils.ProcessTracking;
using Xunit;

namespace Fluxzy.Tests.Cases
{
    public class CustomProcessResolverTests
    {
        private sealed class FixedResolver : IProcessInfoResolver
        {
            private readonly ProcessInfo _info;

            public FixedResolver(ProcessInfo info) => _info = info;

            public ProcessResolutionContext? LastContext { get; private set; }

            public ValueTask<ProcessInfo?> ResolveAsync(ProcessResolutionContext context, CancellationToken token)
            {
                LastContext = context;
                return new ValueTask<ProcessInfo?>(_info);
            }
        }

        [Fact]
        public async Task CustomResolver_IsUsed_WhenProcessTrackingEnabled()
        {
            var sentinel = new ProcessInfo(424242, "/opt/tun2socks/app", "app --tunnel");
            var resolver = new FixedResolver(sentinel);

            await using var proxy = new AddHocProxy(
                expectedRequestCount: 1,
                timeoutSeconds: 30,
                configureSetting: setting => setting.SetEnableProcessTracking(true),
                processInfoResolver: resolver);

            using var clientHandler = new HttpClientHandler
            {
                Proxy = new WebProxy($"http://{proxy.BindHost}:{proxy.BindPort}")
            };

            using var httpClient = new HttpClient(clientHandler);

            var response = await httpClient.GetAsync(TestConstants.Http11Host);
            response.EnsureSuccessStatusCode();

            await proxy.WaitUntilDone();

            var exchange = proxy.CapturedExchanges.FirstOrDefault();
            Assert.NotNull(exchange);
            Assert.NotNull(exchange.ProcessInfo);
            Assert.Equal(sentinel.ProcessId, exchange.ProcessInfo!.ProcessId);
            Assert.Equal(sentinel.ProcessPath, exchange.ProcessInfo.ProcessPath);

            // The resolver receives the connection identity, including the requested destination.
            Assert.NotNull(resolver.LastContext);
            Assert.Equal(proxy.BindPort, resolver.LastContext!.LocalEndPoint.Port);
            Assert.True(IPAddress.IsLoopback(resolver.LastContext.RemoteEndPoint.Address));
            Assert.Equal("sandbox.fluxzy.io", resolver.LastContext.RequestedAuthority.HostName);
            Assert.Equal(443, resolver.LastContext.RequestedAuthority.Port);
        }

        [Fact]
        public async Task CustomResolver_NotConsulted_WhenProcessTrackingDisabled()
        {
            var resolver = new FixedResolver(new ProcessInfo(1, "x", null));

            await using var proxy = new AddHocProxy(
                expectedRequestCount: 1,
                timeoutSeconds: 30,
                processInfoResolver: resolver);

            using var clientHandler = new HttpClientHandler
            {
                Proxy = new WebProxy($"http://{proxy.BindHost}:{proxy.BindPort}")
            };

            using var httpClient = new HttpClient(clientHandler);

            var response = await httpClient.GetAsync(TestConstants.Http11Host);
            response.EnsureSuccessStatusCode();

            await proxy.WaitUntilDone();

            var exchange = proxy.CapturedExchanges.FirstOrDefault();
            Assert.NotNull(exchange);
            Assert.Null(exchange.ProcessInfo);
            Assert.Null(resolver.LastContext);
        }
    }
}
