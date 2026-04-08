// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Text.Json;
using System.Threading.Tasks;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.Cases;

/// <summary>
///     Regression coverage for GitHub issue #610: H/2 ALPN must be offered when running
///     in reverse-secure mode AND the global --serve-h2 (FluxzySetting.ServeH2) option is on.
/// </summary>
public class ReverseProxyH2Tests
{
    [Fact]
    public async Task ReverseMode_WithServeH2_ClientNegotiatesH2()
    {
        // Arrange: Kestrel backend that natively speaks h2 over TLS.
        await using var host = await InProcessHost.Create();

        var setting = FluxzySetting.CreateLocalRandomPort();
        setting.SetReverseMode(true);
        setting.SetReverseModeForcedPort(host.Port);
        setting.SetServeH2(true);
        setting.AddAlterationRules(
            new SkipRemoteCertificateValidationAction(), AnyFilter.Default);
        setting.AddAlterationRules(
            new AddResponseHeaderAction("X-Fluxzy-Reverse-H2", "true"), AnyFilter.Default);

        await using var proxy = new Proxy(setting);
        var proxyEndPoint = proxy.Run().First();
        var proxyPort = proxyEndPoint.Port;

        using var client = CreateReverseProxyClient(proxyPort, httpVersion: HttpVersion.Version20);

        // Act
        var response = await client.GetAsync("/hello");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(HttpVersion.Version20, response.Version);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Hello from Kestrel!", doc.RootElement.GetProperty("message").GetString());

        Assert.True(response.Headers.TryGetValues("X-Fluxzy-Reverse-H2", out var values));
        Assert.Equal("true", values!.First());
    }

    [Fact]
    public async Task ReverseMode_WithoutServeH2_StaysHttp11()
    {
        // Regression guard: when --serve-h2 is NOT set, reverse-secure mode must keep
        // advertising only http/1.1 so existing clients do not regress.
        await using var host = await InProcessHost.Create();

        var setting = FluxzySetting.CreateLocalRandomPort();
        setting.SetReverseMode(true);
        setting.SetReverseModeForcedPort(host.Port);
        // Deliberately NO SetServeH2(true)
        setting.AddAlterationRules(
            new SkipRemoteCertificateValidationAction(), AnyFilter.Default);

        await using var proxy = new Proxy(setting);
        var proxyEndPoint = proxy.Run().First();
        var proxyPort = proxyEndPoint.Port;

        // Client advertises both h2 and http/1.1 in ALPN. Fluxzy must pick http/1.1.
        using var client = CreateReverseProxyClient(proxyPort, httpVersion: HttpVersion.Version11);

        var response = await client.GetAsync("/hello");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(HttpVersion.Version11, response.Version);
    }

    /// <summary>
    ///     Builds an HttpClient that reaches Fluxzy in reverse-secure mode: the TLS handshake
    ///     is performed against Fluxzy (SNI = "localhost") but ApplicationProtocols includes h2.
    /// </summary>
    private static HttpClient CreateReverseProxyClient(int proxyPort, Version httpVersion)
    {
        var handler = new SocketsHttpHandler
        {
            SslOptions = new SslClientAuthenticationOptions
            {
                TargetHost = "localhost",
                RemoteCertificateValidationCallback = (_, _, _, _) => true,
                ApplicationProtocols = new List<SslApplicationProtocol>
                {
                    SslApplicationProtocol.Http2,
                    SslApplicationProtocol.Http11,
                }
            }
        };

        return new HttpClient(handler)
        {
            BaseAddress = new Uri($"https://localhost:{proxyPort}"),
            DefaultRequestVersion = httpVersion,
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact,
        };
    }
}
