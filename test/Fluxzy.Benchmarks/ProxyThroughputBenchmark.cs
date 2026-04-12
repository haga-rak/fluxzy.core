using System.Collections.Concurrent;
using System.Diagnostics.Tracing;
using System.Globalization;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Fluxzy.Rules.Actions;
using Fluxzy.Tests._Fixtures;
using Microsoft.Diagnostics.NETCore.Client;

namespace Fluxzy.Benchmarks;

/// <summary>
///     Proxy throughput benchmark that simulates the floody test scenario:
///     floody → SOCKS5 → Fluxzy Proxy (H2 or H1.1) → Kestrel HTTPS Server
///
///     Run: dotnet run -c Release -- --filter *ProxyThroughputBenchmark*
/// </summary>
[MemoryDiagnoser]
//[ThreadingDiagnoser]
[Config(typeof(Config))]
public class ProxyThroughputBenchmark
{
    private const int RequestsPerIteration = 500;
    private const int Concurrency = 56;

    private BenchmarkServerProcess _server = null!;
    private Proxy _proxy = null!;
    private HttpClient _client = null!;
    private SemaphoreSlim _semaphore = null!;
    private string _targetUrl = null!;
    private ByteCounter _byteCounter = null!;

    [Params(true, false)]
    public bool ServeH2 { get; set; }

    [Params(0, 8192)]
    public int ResponseBodyLength { get; set; }

    [GlobalSetup]
    public async Task Setup()
    {
        // 1. Start external HTTPS test server (separate process to avoid polluting memory measurements)
        _server = new BenchmarkServerProcess();
        await _server.StartAsync();

        // 2. Start Fluxzy proxy (equivalent to: fluxzy start -k --serve-h2)
        var setting = FluxzySetting.CreateLocalRandomPort();
        setting.SetServeH2(ServeH2);
        setting.ConfigureRule()
            .WhenAny()
            .Do(new SkipRemoteCertificateValidationAction()); // -k flag

        setting.SetConnectionPerHost(63);

        _proxy = new Proxy(setting);
        var endPoint = _proxy.Run().First();

        // 3. Create HTTP client routed through proxy via SOCKS5
        //    (equivalent to: floody -c 16 -x 127.0.0.1:<port>)
        //    The counting stream wraps the raw socket — TLS/HTTP layers sit above it,
        //    so the counter tallies actual TCP bytes (encrypted, includes TLS framing).
        var httpVersion = ServeH2 ? new Version(2, 0) : new Version(1, 1);
        _byteCounter = new ByteCounter();
        _client = Socks5ClientFactory.Create(
            endPoint,
            timeoutSeconds: 30,
            httpVersion: httpVersion,
            streamWrapper: s => new CountingStream(s, _byteCounter));

        _targetUrl = $"{_server.BaseUrl}/bench?length={ResponseBodyLength}";
        _semaphore = new SemaphoreSlim(Concurrency);

        // Warmup: establish connections (pays SOCKS5 + TLS handshake costs once)
        var warmupTasks = new Task[Concurrency];

        for (var i = 0; i < Concurrency; i++) {
            warmupTasks[i] = SendRequest();
        }

        await Task.WhenAll(warmupTasks);

        // Measure real per-request wire bytes on already-warm connections,
        // then persist for the BandwidthColumn (which runs in the host process).
        await MeasureWireBytesPerRequestAsync();
    }

    private async Task MeasureWireBytesPerRequestAsync()
    {
        const int probeCount = 32;

        var inBefore = Interlocked.Read(ref _byteCounter.BytesIn);
        var outBefore = Interlocked.Read(ref _byteCounter.BytesOut);

        var probeTasks = new Task[probeCount];

        for (var i = 0; i < probeCount; i++) {
            probeTasks[i] = SendRequest();
        }

        await Task.WhenAll(probeTasks);

        var inAfter = Interlocked.Read(ref _byteCounter.BytesIn);
        var outAfter = Interlocked.Read(ref _byteCounter.BytesOut);

        var totalBytes = (inAfter - inBefore) + (outAfter - outBefore);
        var bytesPerRequest = (double) totalBytes / probeCount;

        var path = GetMeasurementFilePath(ServeH2, ResponseBodyLength);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, bytesPerRequest.ToString("R", CultureInfo.InvariantCulture));
    }

    private static string GetMeasurementFilePath(bool serveH2, int responseBodyLength)
    {
        return Path.Combine(
            Path.GetTempPath(),
            "fluxzy-bench",
            $"bandwidth-{serveH2}-{responseBodyLength}.txt");
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
        using var response = await _client.GetAsync(_targetUrl, HttpCompletionOption.ResponseContentRead)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        // Drain the response body using pooled buffers (no per-request allocations)
        await response.Content.CopyToAsync(Stream.Null).ConfigureAwait(false);
    }

    private class Config : ManualConfig
    {
        // CLR ETW keywords — values come from Microsoft-Windows-DotNETRuntime provider manifest.
        private const long ClrGcKeyword = 0x1;                // GC/AllocationTick, GCHeapStats, GCTriggered
        private const long ClrGcHandleKeyword = 0x2;          // GC handle traffic (pinning, weak refs)
        private const long ClrLoaderKeyword = 0x8;
        private const long ClrJitKeyword = 0x10;
        private const long ClrContentionKeyword = 0x4000;
        private const long ClrTypeKeyword = 0x80000;          // type name resolution for alloc events
        private const long ClrJitToNativeMapKeyword = 0x20000;
        private const long ClrStackKeyword = 0x40000000;

        public Config()
        {
            WithSummaryStyle(SummaryStyle.Default.WithRatioStyle(RatioStyle.Percentage));
            AddColumn(StatisticColumn.OperationsPerSecond);
            AddColumn(new BandwidthColumn());

            // Opt-in contention trace: FLUXZY_BENCH_CONTENTION=1 produces a .nettrace per benchmark
            // run (in BenchmarkDotNet.Artifacts/), openable in PerfView / VS / speedscope.
            if (string.Equals(
                    Environment.GetEnvironmentVariable("FLUXZY_BENCH_CONTENTION"),
                    "1",
                    StringComparison.Ordinal)) {
                var providers = new[] {
                    new EventPipeProvider(
                        name: "Microsoft-Windows-DotNETRuntime",
                        eventLevel: EventLevel.Verbose,
                        keywords: ClrContentionKeyword
                                  | ClrJitKeyword
                                  | ClrLoaderKeyword
                                  | ClrJitToNativeMapKeyword
                                  | ClrStackKeyword)
                };

                AddDiagnoser(new EventPipeProfiler(providers: providers));
            }

            // Opt-in allocation trace: FLUXZY_BENCH_ALLOC=1 produces a .nettrace per benchmark run
            // with sampled GC/AllocationTick events (~every 100 KB of allocations) and managed
            // call stacks. Open in PerfView ("GC Heap Alloc Ignore Free (Coarse Sampling) Stacks"),
            // Visual Studio, or convert with `dotnet-trace convert --format speedscope *.nettrace`.
            if (string.Equals(
                    Environment.GetEnvironmentVariable("FLUXZY_BENCH_ALLOC"),
                    "1",
                    StringComparison.Ordinal)) {
                var providers = new[] {
                    new EventPipeProvider(
                        name: "Microsoft-Windows-DotNETRuntime",
                        eventLevel: EventLevel.Verbose,
                        keywords: ClrGcKeyword
                                  | ClrGcHandleKeyword
                                  | ClrTypeKeyword
                                  | ClrJitKeyword
                                  | ClrLoaderKeyword
                                  | ClrJitToNativeMapKeyword
                                  | ClrStackKeyword)
                };

                AddDiagnoser(new EventPipeProfiler(providers: providers));
            }
        }
    }

    /// <summary>
    ///     Reports real wire bandwidth using the per-request byte count measured during
    ///     <see cref="MeasureWireBytesPerRequestAsync"/>. The measurement counts actual
    ///     TCP bytes (post-TLS encryption, post-SOCKS5 framing) for steady-state requests
    ///     on already-warm connections, then this column multiplies by ops/sec to get
    ///     bytes/sec for each benchmark case.
    /// </summary>
    private class BandwidthColumn : IColumn
    {
        private static readonly ConcurrentDictionary<string, double?> Cache = new();

        public string Id => nameof(BandwidthColumn);
        public string ColumnName => "Bandwidth";
        public string Legend => "Real wire throughput (measured bytes/request × ops/sec)";
        public bool AlwaysShow => true;
        public ColumnCategory Category => ColumnCategory.Statistics;
        public int PriorityInCategory => 0;
        public bool IsNumeric => true;
        public UnitType UnitType => UnitType.Size;

        public bool IsAvailable(Summary summary) => true;
        public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
            => GetValue(summary, benchmarkCase, SummaryStyle.Default);

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style)
        {
            var report = summary[benchmarkCase];

            if (report?.ResultStatistics == null)
                return "-";

            var serveH2 = benchmarkCase.Parameters.Items
                .FirstOrDefault(p => p.Name == nameof(ServeH2))?.Value as bool?;
            var bodyLength = benchmarkCase.Parameters.Items
                .FirstOrDefault(p => p.Name == nameof(ResponseBodyLength))?.Value as int?;

            if (serveH2 is null || bodyLength is null)
                return "-";

            var bytesPerRequest = LoadMeasurement(serveH2.Value, bodyLength.Value);

            if (bytesPerRequest is null or <= 0)
                return "-";

            // Mean is per-op time in nanoseconds (BDN already divided by OperationsPerInvoke).
            var meanNanoseconds = report.ResultStatistics.Mean;

            if (meanNanoseconds <= 0)
                return "-";

            var opsPerSecond = 1_000_000_000d / meanNanoseconds;
            var bytesPerSecond = opsPerSecond * bytesPerRequest.Value;

            return FormatBytesPerSecond(bytesPerSecond);
        }

        private static double? LoadMeasurement(bool serveH2, int bodyLength)
        {
            var key = $"{serveH2}_{bodyLength}";

            return Cache.GetOrAdd(key, _ => {
                var path = GetMeasurementFilePath(serveH2, bodyLength);

                if (!File.Exists(path))
                    return null;

                try {
                    var text = File.ReadAllText(path).Trim();

                    if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                        return value;
                }
                catch {
                    // ignore — column degrades to "-"
                }

                return null;
            });
        }

        private static string FormatBytesPerSecond(double bytesPerSecond)
        {
            string[] units = { "B/s", "KiB/s", "MiB/s", "GiB/s" };
            var unitIndex = 0;
            var value = bytesPerSecond;

            while (value >= 1024d && unitIndex < units.Length - 1) {
                value /= 1024d;
                unitIndex++;
            }

            return value.ToString("F2", CultureInfo.InvariantCulture) + " " + units[unitIndex];
        }
    }

    /// <summary>
    ///     Holds running totals of bytes read from / written to the underlying TCP socket.
    ///     Updated by <see cref="CountingStream"/> via interlocked adds so it's safe to
    ///     share across the connection pool.
    /// </summary>
    private sealed class ByteCounter
    {
        public long BytesIn;
        public long BytesOut;
    }

    /// <summary>
    ///     Pass-through Stream that tallies bytes read and written into a shared
    ///     <see cref="ByteCounter"/>. Wraps the raw NetworkStream below the TLS layer,
    ///     so the count reflects actual TCP wire bytes including TLS/SOCKS5 framing.
    /// </summary>
    private sealed class CountingStream : Stream
    {
        private readonly Stream _inner;
        private readonly ByteCounter _counter;

        public CountingStream(Stream inner, ByteCounter counter)
        {
            _inner = inner;
            _counter = counter;
        }

        public override bool CanRead => _inner.CanRead;
        public override bool CanWrite => _inner.CanWrite;
        public override bool CanSeek => false;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() => _inner.Flush();

        public override Task FlushAsync(CancellationToken cancellationToken)
            => _inner.FlushAsync(cancellationToken);

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count)
        {
            var n = _inner.Read(buffer, offset, count);

            if (n > 0)
                Interlocked.Add(ref _counter.BytesIn, n);

            return n;
        }

        public override async ValueTask<int> ReadAsync(
            Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var n = await _inner.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);

            if (n > 0)
                Interlocked.Add(ref _counter.BytesIn, n);

            return n;
        }

        public override async Task<int> ReadAsync(
            byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var n = await _inner.ReadAsync(buffer.AsMemory(offset, count), cancellationToken)
                .ConfigureAwait(false);

            if (n > 0)
                Interlocked.Add(ref _counter.BytesIn, n);

            return n;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _inner.Write(buffer, offset, count);
            Interlocked.Add(ref _counter.BytesOut, count);
        }

        public override async ValueTask WriteAsync(
            ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await _inner.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
            Interlocked.Add(ref _counter.BytesOut, buffer.Length);
        }

        public override async Task WriteAsync(
            byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await _inner.WriteAsync(buffer.AsMemory(offset, count), cancellationToken)
                .ConfigureAwait(false);
            Interlocked.Add(ref _counter.BytesOut, count);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _inner.Dispose();

            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            await _inner.DisposeAsync().ConfigureAwait(false);
            await base.DisposeAsync().ConfigureAwait(false);
        }
    }
}
