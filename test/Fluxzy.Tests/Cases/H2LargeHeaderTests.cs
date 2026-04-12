// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Threading.Tasks;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters;
using Fluxzy.Tests._Fixtures;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Fluxzy.Tests.Cases;

/// <summary>
///     Regression coverage for the ArrayPool-backed header accumulation buffer in
///     StreamWorker (client-side H2) and ServerStreamWorker (server-side H2).
///
///     The buffers were changed from per-stream `new byte[MaxHeaderSize]` to
///     rent-from-<see cref="System.Buffers.ArrayPool{T}"/> + Return-on-Dispose.
///     The regression risks are:
///
///       1. Double-return / returning an already-returned buffer (silent pool corruption).
///       2. Reading from the buffer after Dispose returned it (garbled headers).
///       3. Grow path bug: lost bytes when re-renting a larger array and copying the prefix.
///
///     These tests force the grow path (response headers > default 16 KB MaxHeaderSize),
///     run many sequential requests so rent/return churn is high, and verify that the
///     decoded headers the client observes match exactly what the backend sent. Any of
///     the regression modes above would surface as header corruption or missing fields.
/// </summary>
public class H2LargeHeaderTests
{
    /// <summary>
    ///     Backend emits ~30 KB of response headers across 30 fields — above Fluxzy's
    ///     default MaxHeaderSize (16 384 B) so the proxy's StreamWorker must take the
    ///     grow path (initial 16 KB rent → grow → copy → Return old). Runs 20 sequential
    ///     requests so the pool sees rent/return churn, not a one-off. Any mis-return
    ///     would corrupt a later request's decoded headers.
    /// </summary>
    [Fact]
    public async Task LargeResponseHeaders_GrowPath_HeadersIntactAcrossManyRequests()
    {
        const int headerCount = 30;
        const int headerValueLength = 1024; // ~30 KB total

        await using var host = await InProcessHost.Create(app =>
        {
            app.MapGet("/big-headers", (HttpContext ctx) =>
            {
                for (var i = 0; i < headerCount; i++) {
                    ctx.Response.Headers[$"X-Large-Header-{i:D3}"] = new string((char) ('a' + (i % 26)), headerValueLength);
                }

                return Results.Ok("ok");
            });
        }, suppressLogging: true);

        var setting = FluxzySetting.CreateLocalRandomPort();
        setting.SetReverseMode(true);
        setting.SetReverseModeForcedPort(host.Port);
        setting.SetServeH2(true);
        setting.AddAlterationRules(
            new SkipRemoteCertificateValidationAction(), AnyFilter.Default);

        await using var proxy = new Proxy(setting);
        var proxyPort = proxy.Run().First().Port;

        using var client = CreateClient(proxyPort);

        // Sequential to exercise rent/return cycles on a single StreamWorker lifecycle
        // at a time (easier to reason about) — any double-return would immediately
        // corrupt the pool and a later request would observe garbled content.
        for (var iteration = 0; iteration < 20; iteration++) {
            using var response = await client.GetAsync("/big-headers");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(HttpVersion.Version20, response.Version);

            for (var i = 0; i < headerCount; i++) {
                var headerName = $"X-Large-Header-{i:D3}";
                var expectedChar = (char) ('a' + (i % 26));
                var expectedValue = new string(expectedChar, headerValueLength);

                Assert.True(
                    response.Headers.TryGetValues(headerName, out var values),
                    $"iteration {iteration}: missing header {headerName}");

                var actual = values!.First();
                Assert.Equal(expectedValue.Length, actual.Length);
                Assert.Equal(expectedValue, actual);
            }
        }
    }

    /// <summary>
    ///     Backend emits response headers within the default 16 KB buffer (no grow path),
    ///     but the test runs 50 sequential requests so every iteration relies on a fresh
    ///     buffer rented from the shared pool. Any leak (no Return) would still pass; any
    ///     double-return or early Return would eventually surface as header corruption
    ///     because ArrayPool may hand the same array to two concurrent streams.
    /// </summary>
    [Fact]
    public async Task ResponseHeaders_FastPathRentReturn_StableAcrossSequentialRequests()
    {
        await using var host = await InProcessHost.Create(app =>
        {
            app.MapGet("/api/item/{id:int}", (int id, HttpContext ctx) =>
            {
                ctx.Response.Headers["X-Item-Id"] = id.ToString();
                ctx.Response.Headers["X-Trace"] = new string('z', 512);

                return Results.Ok(new { id, value = "ok" });
            });
        }, suppressLogging: true);

        var setting = FluxzySetting.CreateLocalRandomPort();
        setting.SetReverseMode(true);
        setting.SetReverseModeForcedPort(host.Port);
        setting.SetServeH2(true);
        setting.AddAlterationRules(
            new SkipRemoteCertificateValidationAction(), AnyFilter.Default);

        await using var proxy = new Proxy(setting);
        var proxyPort = proxy.Run().First().Port;

        using var client = CreateClient(proxyPort);

        for (var i = 0; i < 50; i++) {
            using var response = await client.GetAsync($"/api/item/{i}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(HttpVersion.Version20, response.Version);
            Assert.True(response.Headers.TryGetValues("X-Item-Id", out var idValues));
            Assert.Equal(i.ToString(), idValues!.First());

            Assert.True(response.Headers.TryGetValues("X-Trace", out var traceValues));
            Assert.Equal(new string('z', 512), traceValues!.First());
        }
    }

    private static HttpClient CreateClient(int proxyPort)
    {
        var handler = new SocketsHttpHandler
        {
            SslOptions = new SslClientAuthenticationOptions
            {
                TargetHost = "localhost",
                RemoteCertificateValidationCallback = (_, _, _, _) => true,
                ApplicationProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http2 }
            }
        };

        return new HttpClient(handler)
        {
            BaseAddress = new Uri($"https://localhost:{proxyPort}"),
            DefaultRequestVersion = HttpVersion.Version20,
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact,
        };
    }
}
