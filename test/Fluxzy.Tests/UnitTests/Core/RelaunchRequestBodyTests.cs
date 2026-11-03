// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
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
                    using var client = HttpClientUtility.CreateHttpClient(proxy.Run(), setting,
                        handler => handler.ServerCertificateCustomValidationCallback =
                            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator);
                    client.DefaultRequestVersion = HttpVersion.Version11;
                    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;

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
    }
}
