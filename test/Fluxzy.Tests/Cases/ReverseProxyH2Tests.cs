// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients.H2;
using Fluxzy.Clients.H2.Encoder;
using Fluxzy.Clients.H2.Encoder.Utils;
using Fluxzy.Clients.H2.Frames;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters;
using Fluxzy.Tests._Fixtures;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
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
    ///     Regression for the Firefox-style zero-length POST that used to corrupt the
    ///     upstream H2 stream. Firefox sends a POST with Content-Length: 0 as
    ///     HEADERS (no END_STREAM) followed by DATA[END_STREAM, 0 bytes]. Chrome collapses
    ///     both into a single HEADERS[END_STREAM], which is why the bug only reproduces
    ///     with Firefox-shaped clients.
    ///
    ///     The bug: StreamWorker.EnqueueRequestHeader saw Content-Length: 0 and set
    ///     END_STREAM on the upstream HEADERS frame, half-closing the stream. But
    ///     StreamWorker.ProcessRequestBody still entered its body loop (because the
    ///     request body is a non-seekable pipe stream) and enqueued an extra
    ///     DATA[END_STREAM, 0 bytes] on the already half-closed (local) stream. The
    ///     remote answered with a connection reset (PROTOCOL_ERROR / STREAM_CLOSED).
    ///
    ///     This test pipes the Firefox pattern through a raw H2 downstream client into
    ///     Fluxzy, while a strict raw-H2 server on the upstream side records every
    ///     frame it receives. The assertion is simple: the upstream must never see a
    ///     DATA frame after the HEADERS frame already carried END_STREAM.
    /// </summary>
    [Fact]
    public async Task ReverseMode_WithServeH2_FirefoxStyleEmptyPost_UpstreamHasNoStrayDataFrame()
    {
        using var serverCert = InProcessHost.CreateSelfSignedCertificateForTesting();
        await using var strictServer = await StrictH2Recorder.StartAsync(serverCert);

        var setting = FluxzySetting.CreateLocalRandomPort();
        setting.SetReverseMode(true);
        setting.SetReverseModeForcedPort(strictServer.Port);
        setting.SetServeH2(true);
        setting.AddAlterationRules(
            new SkipRemoteCertificateValidationAction(), AnyFilter.Default);

        await using var proxy = new Proxy(setting);
        var proxyPort = proxy.Run().First().Port;

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));

        using var tcp = new TcpClient();
        await tcp.ConnectAsync(IPAddress.Loopback, proxyPort, cts.Token);

        await using var sslStream = new SslStream(tcp.GetStream(), leaveInnerStreamOpen: false);
        await sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
        {
            TargetHost = "localhost",
            RemoteCertificateValidationCallback = (_, _, _, _) => true,
            ApplicationProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http2 },
        }, cts.Token);

        Assert.Equal(SslApplicationProtocol.Http2, sslStream.NegotiatedApplicationProtocol);

        // Client H2 preface + empty SETTINGS.
        await sslStream.WriteAsync(H2Constants.Preface.AsMemory(), cts.Token);
        await sslStream.WriteAsync(H2FrameHelper.BuildSettingsFrame(), cts.Token);
        await sslStream.FlushAsync(cts.Token);

        // Drain frames from Fluxzy until we see its server SETTINGS — then send an ACK.
        await DrainUntilServerSettings(sslStream, cts.Token);
        await sslStream.WriteAsync(H2FrameHelper.BuildSettingsAck(), cts.Token);
        await sslStream.FlushAsync(cts.Token);

        // Firefox pattern for POST /Feeds/Pop with Content-Length: 0:
        //   HEADERS (END_HEADERS, NOT END_STREAM) then DATA[END_STREAM, 0 bytes].
        var encoder = new HPackEncoder(new EncodingContext(ArrayPoolMemoryProvider<char>.Default));
        var plainHeaders = (
            "POST /Feeds/Pop HTTP/2\r\n" +
            "Host: localhost\r\n" +
            "Content-Length: 0\r\n" +
            "TE: trailers\r\n\r\n"
        ).AsMemory();

        await sslStream.WriteAsync(
            H2FrameHelper.BuildHeadersFrame(encoder, 1, plainHeaders, endStream: false, endHeaders: true),
            cts.Token);
        await sslStream.WriteAsync(
            H2FrameHelper.BuildDataFrame(1, Array.Empty<byte>(), endStream: true),
            cts.Token);
        await sslStream.FlushAsync(cts.Token);

        // Expect a HEADERS frame on stream 1 from the downstream side. This only
        // confirms the round-trip finished; the real assertion is on the upstream.
        await WaitForHeadersOnStream(sslStream, targetStreamId: 1, cts.Token);

        // The upstream mock fails the test if it ever saw a DATA frame on a stream
        // that already had END_STREAM set. Tear down the mock and surface any error.
        await strictServer.StopAndVerifyAsync();
    }

    private static async Task DrainUntilServerSettings(Stream stream, CancellationToken token)
    {
        var deadline = Environment.TickCount64 + 5000;

        while (Environment.TickCount64 < deadline) {
            var (bodyType, _, _, _) = await ReadFrameHeaderAsync(stream, token);

            if (bodyType == H2FrameType.Settings)
                return;

            if (bodyType == H2FrameType.Goaway)
                throw new InvalidOperationException("Fluxzy sent GOAWAY during handshake");
        }

        throw new TimeoutException("Did not receive server SETTINGS in time");
    }

    private static async Task WaitForHeadersOnStream(
        Stream stream, int targetStreamId, CancellationToken token)
    {
        while (true) {
            var (bodyType, streamId, _, bodyBytes) = await ReadFrameHeaderAsync(stream, token);

            if (bodyType == H2FrameType.RstStream && streamId == targetStreamId) {
                var errorCode = (H2ErrorCode)BinaryPrimitives.ReadUInt32BigEndian(bodyBytes);
                throw new InvalidOperationException(
                    $"RST_STREAM on stream {targetStreamId}: {errorCode}");
            }

            if (bodyType == H2FrameType.Goaway) {
                var errorCode = (H2ErrorCode)BinaryPrimitives.ReadUInt32BigEndian(bodyBytes.AsSpan(4, 4));
                throw new InvalidOperationException($"GOAWAY: {errorCode}");
            }

            if (bodyType == H2FrameType.Headers && streamId == targetStreamId) {
                // We reached a response HEADERS frame on our stream without any protocol
                // error — that's enough to prove Fluxzy accepted and forwarded the
                // Firefox-shaped empty POST round-trip.
                return;
            }
        }
    }

    private static async Task<(H2FrameType bodyType, int streamId, HeaderFlags flags, byte[] body)>
        ReadFrameHeaderAsync(Stream stream, CancellationToken token)
    {
        var header = new byte[9];
        await ReadExactAsync(stream, header, token);

        var bodyLength = (header[0] << 16) | (header[1] << 8) | header[2];
        var bodyType = (H2FrameType)header[3];
        var flags = (HeaderFlags)header[4];
        var streamId = BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(5, 4)) & 0x7FFFFFFF;

        var body = bodyLength == 0 ? Array.Empty<byte>() : new byte[bodyLength];
        if (bodyLength > 0)
            await ReadExactAsync(stream, body, token);

        return (bodyType, streamId, flags, body);
    }

    private static async Task ReadExactAsync(Stream stream, byte[] buffer, CancellationToken token)
    {
        var offset = 0;
        while (offset < buffer.Length) {
            var n = await stream.ReadAsync(buffer.AsMemory(offset), token);
            if (n == 0)
                throw new EndOfStreamException("Upstream closed the TLS connection unexpectedly");
            offset += n;
        }
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
