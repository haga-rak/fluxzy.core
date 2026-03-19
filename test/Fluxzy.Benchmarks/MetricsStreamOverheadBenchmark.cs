using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Fluxzy.Misc.Streams;

namespace Fluxzy.Benchmarks;

/// <summary>
///     Measures the per-read CancellationTokenSource.CreateLinkedTokenSource allocation
///     in MetricsStream. H2 may trigger more reads per response (smaller frame size = more reads)
///     so this cost compounds.
///
///     Run: dotnet run -c Release -- --filter *MetricsStreamOverhead*
/// </summary>
[MemoryDiagnoser]
public class MetricsStreamOverheadBenchmark
{
    private byte[] _data = null!;
    private byte[] _readBuffer = null!;

    [Params(16384, 65536)]
    public int ReadSize { get; set; }

    [Params(65536, 524288)]
    public int TotalPayload { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _data = new byte[TotalPayload];
        Random.Shared.NextBytes(_data);
        _readBuffer = new byte[ReadSize];
    }

    /// <summary>
    ///     Baseline: read directly from MemoryStream (no MetricsStream wrapper).
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task<long> ReadDirect()
    {
        using var stream = new MemoryStream(_data);
        long total = 0;
        int read;

        while ((read = await stream.ReadAsync(_readBuffer.AsMemory())) > 0) {
            total += read;
        }

        return total;
    }

    /// <summary>
    ///     Read through MetricsStream — each ReadAsync creates a linked CTS.
    /// </summary>
    [Benchmark]
    public async Task<long> ReadWithMetricsStream()
    {
        using var inner = new MemoryStream(_data);

        var metrics = new MetricsStream(
            inner,
            firstBytesRead: static () => { },
            endRead: static (_, _) => { },
            onReadError: static _ => { },
            endConnection: false,
            expectedLength: TotalPayload,
            parentToken: CancellationToken.None);

        long total = 0;
        int read;

        while ((read = await metrics.ReadAsync(_readBuffer.AsMemory())) > 0) {
            total += read;
        }

        return total;
    }

    /// <summary>
    ///     Isolated CTS create + dispose cost (no actual I/O).
    ///     Shows the pure overhead per linked token operation.
    /// </summary>
    [Benchmark]
    public int LinkedCtsCreateDispose()
    {
        var parentCts = new CancellationTokenSource();
        var iterations = TotalPayload / ReadSize;
        var count = 0;

        for (var i = 0; i < iterations; i++) {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(
                parentCts.Token, CancellationToken.None);
            count++;
        }

        parentCts.Dispose();

        return count;
    }
}
