// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Rules.Actions;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.Cases
{
    /// <summary>
    ///     Regression tests for upstream hosts that accept the connection (and the TLS
    ///     handshake) but never send a response header (haga-rak/fluxzy.core#634).
    ///     Before the fix, the exchange parked forever on the upstream read: no response
    ///     header timeout existed, and a downstream client abort was never observed while
    ///     waiting on upstream.
    /// </summary>
    public class UpstreamResponseHeaderHangTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Tarpit_Upstream_Times_Out_With_Response_Header_Timeout(bool useBouncyCastle)
        {
            await using var server = TarpitServer.Start(useTls: true);

            var setting = FluxzySetting.CreateLocalRandomPort();

            if (useBouncyCastle)
                setting.UseBouncyCastleSslEngine();

            setting.SetResponseHeaderTimeout(TimeSpan.FromSeconds(2));

            setting.ConfigureRule().WhenAny()
                   .Do(new SkipRemoteCertificateValidationAction());

            await using var proxy = new Proxy(setting);

            using var client = HttpClientUtility.CreateHttpClient(proxy.Run(), setting);
            client.Timeout = TimeSpan.FromSeconds(40);

            var watch = Stopwatch.StartNew();

            using var response = await client.GetAsync($"https://local.fluxzy.io:{server.Port}/");

            watch.Stop();

            Assert.Equal(528, (int) response.StatusCode);

            Assert.True(response.Headers.TryGetValues("x-fluxzy-network-error", out var errorCodes));
            Assert.Equal(NetworkErrorCodes.ResponseHeaderTimeout, errorCodes!.First());

            Assert.True(watch.Elapsed < TimeSpan.FromSeconds(20),
                $"Timeout was configured at 2s but the response took {watch.Elapsed}");

            Assert.True(await server.WaitForTeardown(TimeSpan.FromSeconds(10)),
                "The hung upstream connection should be aborted when the timeout fires");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Client_Abort_Tears_Down_Hung_Upstream(bool useTls)
        {
            await using var server = TarpitServer.Start(useTls);

            var setting = FluxzySetting.CreateLocalRandomPort();

            setting.SetResponseHeaderTimeout(TimeSpan.FromSeconds(120));

            setting.ConfigureRule().WhenAny()
                   .Do(new SkipRemoteCertificateValidationAction());

            await using var proxy = new Proxy(setting);

            using var client = HttpClientUtility.CreateHttpClient(proxy.Run(), setting);
            client.Timeout = TimeSpan.FromSeconds(2);

            var url = useTls
                ? $"https://local.fluxzy.io:{server.Port}/"
                : $"http://127.0.0.1:{server.Port}/";

            await Assert.ThrowsAsync<TaskCanceledException>(() => client.GetAsync(url));

            Assert.True(await server.WaitForTeardown(TimeSpan.FromSeconds(10)),
                "The hung upstream connection should be torn down shortly after the " +
                "downstream client aborts, instead of parking until the header timeout");
        }

        [Fact]
        public async Task Slow_Response_Within_Timeout_Succeeds()
        {
            await using var server = TarpitServer.Start(useTls: true, respondAfter: TimeSpan.FromSeconds(3));

            var setting = FluxzySetting.CreateLocalRandomPort();

            setting.SetResponseHeaderTimeout(TimeSpan.FromSeconds(30));

            setting.ConfigureRule().WhenAny()
                   .Do(new SkipRemoteCertificateValidationAction());

            await using var proxy = new Proxy(setting);

            using var client = HttpClientUtility.CreateHttpClient(proxy.Run(), setting);
            client.Timeout = TimeSpan.FromSeconds(40);

            using var response = await client.GetAsync($"https://local.fluxzy.io:{server.Port}/");
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("ok", body);
        }
    }
}
