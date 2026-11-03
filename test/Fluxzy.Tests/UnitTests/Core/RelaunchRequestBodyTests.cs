// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Readers;
using Fluxzy.Rules.Actions;
using Fluxzy.Tests._Fixtures;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Xunit;
using Xunit.Abstractions;

namespace Fluxzy.Tests.UnitTests.Core
{
    public class RelaunchRequestBodyTests
    {
        private readonly ITestOutputHelper _output;

        public RelaunchRequestBodyTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task Put_Relaunched_On_Idle_Closed_Connection_Still_Carries_Body()
        {
            var expectedBody = "{\"etage\":\"ETAGE_INTERMEDIAIRE\"}";
            var connectionIds = new ConcurrentDictionary<string, byte>();
            var putCount = 0;
            var warmConnectionClosed = new TaskCompletionSource();
            var started = DateTime.Now;

            await using var origin = await InProcessHost.Create(app => {
                app.MapGet("/warm", (HttpContext context) => {
                    connectionIds.TryAdd(context.Connection.Id, 0);
                    context.Features.Get<IConnectionLifetimeFeature>()!
                           .ConnectionClosed.Register(() => warmConnectionClosed.TrySetResult());
                    return Results.Text("ok");
                });

                app.MapPut("/put", async (HttpContext context) => {
                    connectionIds.TryAdd(context.Connection.Id, 0);
                    Interlocked.Increment(ref putCount);
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                    using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);

                    try {
                        var body = await reader.ReadToEndAsync(cts.Token);
                        return Results.Text(body);
                    }
                    catch (OperationCanceledException) {
                        return Results.Text("body never arrived", statusCode: 400);
                    }
                });
            }, suppressLogging: true, protocols: HttpProtocols.Http1,
                configureKestrel: k => k.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(1));

            var outDirectory = Path.Combine(Path.GetTempPath(), "fluxzy-relaunch-" + Guid.NewGuid().ToString("N"));

            var setting = FluxzySetting.CreateLocalRandomPort();
            setting.SetOutDirectory(outDirectory);
            setting.ConfigureRule().WhenAny()
                   .Do(new SkipRemoteCertificateValidationAction());

            try {
                await using (var proxy = new Proxy(setting)) {
                    using var client = CreateClient(proxy, setting);

                    using var warm = await client.GetAsync($"https://localhost:{origin.Port}/warm");
                    Assert.Equal(HttpStatusCode.OK, warm.StatusCode);

                    await warmConnectionClosed.Task.WaitAsync(TimeSpan.FromSeconds(10));
                    _output.WriteLine($"origin closed idle connection after {(DateTime.Now - started).TotalMilliseconds}ms");

                    using var content = new StringContent(expectedBody, Encoding.UTF8, "application/json");
                    using var response = await client.PutAsync($"https://localhost:{origin.Port}/put", content);
                    var actualBody = await response.Content.ReadAsStringAsync();

                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.Equal(expectedBody, actualBody);
                    Assert.Equal(2, connectionIds.Count);
                    Assert.Equal(1, putCount);
                }
            }
            finally {
                var reader = new DirectoryArchiveReader(outDirectory);

                foreach (var exchange in reader.ReadAllExchanges().OrderBy(e => e.Id)) {
                    var metrics = exchange.Metrics;
                    _output.WriteLine(
                        $"#{exchange.Id} {exchange.Method} {exchange.Path} -> {exchange.StatusCode} " +
                        $"conn={exchange.ConnectionId} reusing={metrics.ReusingConnection} " +
                        $"totalSent={metrics.TotalSent} headerLen={metrics.RequestHeaderLength} " +
                        $"bodyDelay={(metrics.RequestBodySent - metrics.RequestHeaderSent).TotalMilliseconds}ms");
                }

                Directory.Delete(outDirectory, true);
            }
        }

        [Fact]
        public async Task Put_Relaunched_After_Body_Was_Sent_On_Dead_Connection_Replays_Body()
        {
            await using var origin = await AbortingOrigin.Create(readBeforeAbort: -1);

            var setting = FluxzySetting.CreateLocalRandomPort();
            setting.ConfigureRule().WhenAny()
                   .Do(new SkipRemoteCertificateValidationAction());

            await using var proxy = new Proxy(setting);
            using var client = CreateClient(proxy, setting);

            await origin.Warm(client);

            await using var body = new RandomDataStream(11, 64 * 1024);
            using var content = new StreamContent(body, 8192);
            content.Headers.ContentLength = 64 * 1024;

            using var response = await client.PutAsync(origin.PutUrl, content);
            var actual = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal($"{64 * 1024}:{body.Hash}", actual);
            Assert.Equal(2, origin.Attempts);
        }

        [Fact]
        public async Task Chunked_Put_Relaunched_After_Dead_Connection_Replays_Body()
        {
            await using var origin = await AbortingOrigin.Create(readBeforeAbort: -1);

            var setting = FluxzySetting.CreateLocalRandomPort();
            setting.ConfigureRule().WhenAny()
                   .Do(new SkipRemoteCertificateValidationAction());

            await using var proxy = new Proxy(setting);
            using var client = CreateClient(proxy, setting);

            await origin.Warm(client);

            await using var body = new RandomDataStream(13, 64 * 1024 + 123);
            using var content = new StreamContent(body, 8192);

            using var response = await client.PutAsync(origin.PutUrl, content);
            var actual = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal($"{64 * 1024 + 123}:{body.Hash}", actual);
            Assert.Equal(2, origin.Attempts);
            Assert.True(origin.LastRequestWasChunked);
        }

        [Fact]
        public async Task Put_Larger_Than_Replay_Limit_Is_Not_Relaunched()
        {
            const int bodyLength = 1024 * 1024;

            await using var origin = await AbortingOrigin.Create(readBeforeAbort: -1);

            var outDirectory = Path.Combine(Path.GetTempPath(), "fluxzy-relaunch-" + Guid.NewGuid().ToString("N"));

            var setting = FluxzySetting.CreateLocalRandomPort();
            setting.SetOutDirectory(outDirectory);
            setting.ConfigureRule().WhenAny()
                   .Do(new SkipRemoteCertificateValidationAction());

            try {
                await using (var proxy = new Proxy(setting)) {
                    using var client = CreateClient(proxy, setting);

                    await origin.Warm(client);

                    await using var body = new RandomDataStream(17, bodyLength);
                    using var content = new StreamContent(body, 8192);
                    content.Headers.ContentLength = bodyLength;

                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                    using var response = await client.PutAsync(origin.PutUrl, content, cts.Token);
                    var text = await response.Content.ReadAsStringAsync(cts.Token);

                    Assert.Equal((HttpStatusCode) 528, response.StatusCode);
                    Assert.Contains("replayed", text);
                    Assert.Equal(1, origin.Attempts);
                }

                var reader = new DirectoryArchiveReader(outDirectory);
                var put = reader.ReadAllExchanges().Single(e => e.Method == "PUT");

                Assert.Equal(Fluxzy.Core.NetworkErrorCodes.ConnectionClosed, put.ClientErrors.Single().NetworkErrorCode);
                Assert.Equal(put.Metrics.RequestHeaderLength + bodyLength, put.Metrics.TotalSent);
            }
            finally {
                Directory.Delete(outDirectory, true);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(50 * 1024)]
        [InlineData(199 * 1024)]
        public async Task Put_Relaunched_After_Partial_Consumption_Replays_Whole_Body(int readBeforeAbort)
        {
            const int bodyLength = 200 * 1024;

            await using var origin = await AbortingOrigin.Create(readBeforeAbort);

            var setting = FluxzySetting.CreateLocalRandomPort();
            setting.ConfigureRule().WhenAny()
                   .Do(new SkipRemoteCertificateValidationAction());

            await using var proxy = new Proxy(setting);
            using var client = CreateClient(proxy, setting);

            await origin.Warm(client);

            await using var body = new RandomDataStream(19 + readBeforeAbort, bodyLength);
            using var content = new StreamContent(body, 8192);
            content.Headers.ContentLength = bodyLength;

            using var response = await client.PutAsync(origin.PutUrl, content);
            var actual = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal($"{bodyLength}:{body.Hash}", actual);
            Assert.Equal(2, origin.Attempts);
        }

        private static HttpClient CreateClient(Proxy proxy, FluxzySetting setting)
        {
            var client = HttpClientUtility.CreateHttpClient(proxy.Run(), setting,
                handler => handler.ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator);

            client.DefaultRequestVersion = HttpVersion.Version11;
            client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;

            return client;
        }

        private sealed class AbortingOrigin : IAsyncDisposable
        {
            private readonly InProcessHost _host;
            private int _attempts;

            private AbortingOrigin(InProcessHost host)
            {
                _host = host;
            }

            public int Attempts => _attempts;

            public bool LastRequestWasChunked { get; private set; }

            public string PutUrl => $"https://localhost:{_host.Port}/put";

            public static async Task<AbortingOrigin> Create(int readBeforeAbort)
            {
                AbortingOrigin origin = null!;

                var host = await InProcessHost.Create(app => {
                    app.MapGet("/warm", () => Results.Text("ok"));

                    app.MapPut("/put", async (HttpContext context) => {
                        var attempt = Interlocked.Increment(ref origin._attempts);
                        origin.LastRequestWasChunked = context.Request.Headers.TransferEncoding
                                                              .Any(v => v?.Contains("chunked") == true);

                        if (attempt == 1) {
                            await ReadUpTo(context.Request.Body, readBeforeAbort);
                            context.Features.Get<IConnectionLifetimeFeature>()!.Abort();
                            return Results.Empty;
                        }

                        var (length, hash) = await HashToEnd(context.Request.Body);
                        return Results.Text($"{length}:{hash}");
                    });
                }, suppressLogging: true, protocols: HttpProtocols.Http1);

                origin = new AbortingOrigin(host);
                return origin;
            }

            public async Task Warm(HttpClient client)
            {
                using var warm = await client.GetAsync($"https://localhost:{_host.Port}/warm");
                Assert.Equal(HttpStatusCode.OK, warm.StatusCode);
            }

            private static async Task ReadUpTo(Stream stream, int count)
            {
                var buffer = new byte[16 * 1024];
                long total = 0;

                while (count < 0 || total < count) {
                    var toRead = count < 0 ? buffer.Length : (int) Math.Min(buffer.Length, count - total);
                    var read = await stream.ReadAsync(buffer.AsMemory(0, toRead));

                    if (read == 0)
                        return;

                    total += read;
                }
            }

            private static async Task<(long, string)> HashToEnd(Stream stream)
            {
                using var sha = SHA1.Create();
                var buffer = new byte[16 * 1024];
                long total = 0;
                int read;

                while ((read = await stream.ReadAsync(buffer)) > 0) {
                    sha.TransformBlock(buffer, 0, read, null, 0);
                    total += read;
                }

                sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                return (total, Convert.ToHexString(sha.Hash!));
            }

            public ValueTask DisposeAsync()
            {
                return _host.DisposeAsync();
            }
        }
    }
}
