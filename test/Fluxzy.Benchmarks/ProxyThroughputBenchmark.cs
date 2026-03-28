using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using Fluxzy.Rules;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters;
using Fluxzy.Tests._Fixtures;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Fluxzy.Benchmarks;

/// <summary>
///     Proxy throughput benchmark that simulates the floody test scenario:
///     floody → SOCKS5 → Fluxzy Proxy (H2 or H1.1) → Kestrel HTTPS Server
///
///     Run: dotnet run -c Release -- --filter *ProxyThroughputBenchmark*
/// </summary>
[MemoryDiagnoser]
[Config(typeof(Config))]
public class ProxyThroughputBenchmark
{
    private const int RequestsPerIteration = 500;
    private const int Concurrency = 16;

    private InProcessHost _server = null!;
    private Proxy _proxy = null!;
    private HttpClient _client = null!;
    private SemaphoreSlim _semaphore = null!;
    private string _targetUrl = null!;

    [Params(true, false)]
    public bool ServeH2 { get; set; }

    [Params(0, 8192)]
    public int ResponseBodyLength { get; set; }

    [GlobalSetup]
    public async Task Setup()
    {
        // 1. Start HTTPS test server (equivalent to floodys)
        _server = await InProcessHost.Create(suppressLogging: true, configureRoutes: app => {
            app.MapGet("/bench", async ctx => {
                var lengthStr = ctx.Request.Query["length"].FirstOrDefault();
                var length = lengthStr != null ? int.Parse(lengthStr) : 0;

                if (length > 0) {
                    ctx.Response.ContentLength = length;
                    var buffer = new byte[Math.Min(length, 16384)];
                    var remaining = length;

                    while (remaining > 0) {
                        var toWrite = Math.Min(remaining, buffer.Length);
                        await ctx.Response.Body.WriteAsync(buffer.AsMemory(0, toWrite));
                        remaining -= toWrite;
                    }
                }
            });
        });

        // 2. Start Fluxzy proxy (equivalent to: fluxzy start -k --serve-h2)
        var setting = FluxzySetting.CreateLocalRandomPort();
        setting.SetServeH2(ServeH2);
        setting.ConfigureRule()
            .WhenAny()
            .Do(new SkipRemoteCertificateValidationAction()); // -k flag

        _proxy = new Proxy(setting);
        var endPoint = _proxy.Run().First();

        // 3. Create HTTP client routed through proxy via SOCKS5
        //    (equivalent to: floody -c 16 -x 127.0.0.1:<port>)
        var httpVersion = ServeH2 ? new Version(2, 0) : new Version(1, 1);
        _client = Socks5ClientFactory.Create(endPoint, timeoutSeconds: 30, httpVersion: httpVersion);

        _targetUrl = $"{_server.BaseUrl}/bench?length={ResponseBodyLength}";
        _semaphore = new SemaphoreSlim(Concurrency);

        // Warmup: establish connections
        var warmupTasks = new Task[Concurrency];

        for (var i = 0; i < Concurrency; i++) {
            warmupTasks[i] = SendRequest();
        }

        await Task.WhenAll(warmupTasks);
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        _client?.Dispose();
        _semaphore?.Dispose();

        if (_proxy != null)
            await _proxy.DisposeAsync();

        if (_server != null)
            await _server.DisposeAsync();
    }

    /// <summary>
    ///     Sends RequestsPerIteration concurrent requests through the proxy.
    ///     BenchmarkDotNet divides total time by OperationsPerInvoke to get per-request time.
    ///     Inverse of per-request time × concurrency ≈ requests/sec.
    /// </summary>
    [Benchmark(OperationsPerInvoke = RequestsPerIteration)]
    public async Task Throughput()
    {
        var tasks = new Task[RequestsPerIteration];

        for (var i = 0; i < RequestsPerIteration; i++) {
            await _semaphore.WaitAsync().ConfigureAwait(false);

            tasks[i] = Task.Run(async () => {
                try {
                    await SendRequest().ConfigureAwait(false);
                }
                finally {
                    _semaphore.Release();
                }
            });
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task SendRequest()
    {
        using var response = await _client.GetAsync(_targetUrl).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        // Drain the response body (matching floody behavior)
        if (response.Content.Headers.ContentLength > 0) {
            await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
        }
    }

    private class Config : ManualConfig
    {
        public Config()
        {
            WithSummaryStyle(SummaryStyle.Default.WithRatioStyle(RatioStyle.Percentage));
        }
    }
}
