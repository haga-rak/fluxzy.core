using System;
using System.Buffers;
using BenchmarkDotNet.Attributes;

namespace Fluxzy.Benchmarks;

/// <summary>
///     Measures the cost of per-frame ArrayPool.Rent/Return (H2 style)
///     vs single-buffer reuse (H1.1 style).
///
///     H1.1: allocates one buffer, reuses it across all reads.
///     H2:   rents a new buffer from ArrayPool for each frame, returns after write.
///
///     Run: dotnet run -c Release -- --filter *PerFrameAllocation*
/// </summary>
[MemoryDiagnoser]
public class PerFrameAllocationBenchmark
{
    private byte[] _sourceData = null!;

    [Params(16384, 65536)]
    public int FrameSize { get; set; }

    [Params(8192, 524288)]
    public int PayloadSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _sourceData = new byte[PayloadSize];
        Random.Shared.NextBytes(_sourceData);
    }

    /// <summary>
    ///     H1.1 style: rent one buffer, reuse across all iterations.
    /// </summary>
    [Benchmark(Baseline = true)]
    public int SingleBufferLoop()
    {
        var buffer = ArrayPool<byte>.Shared.Rent(FrameSize);
        var totalCopied = 0;
        var offset = 0;

        while (offset < PayloadSize) {
            var toCopy = Math.Min(FrameSize, PayloadSize - offset);
            _sourceData.AsSpan(offset, toCopy).CopyTo(buffer);
            offset += toCopy;
            totalCopied += toCopy;
        }

        ArrayPool<byte>.Shared.Return(buffer);

        return totalCopied;
    }

    /// <summary>
    ///     H2 style: rent and return a buffer for every frame.
    /// </summary>
    [Benchmark]
    public int PerFrameRentReturn()
    {
        var totalCopied = 0;
        var offset = 0;

        while (offset < PayloadSize) {
            var toCopy = Math.Min(FrameSize, PayloadSize - offset);
            var buffer = ArrayPool<byte>.Shared.Rent(FrameSize);
            _sourceData.AsSpan(offset, toCopy).CopyTo(buffer);
            offset += toCopy;
            totalCopied += toCopy;
            ArrayPool<byte>.Shared.Return(buffer);
        }

        return totalCopied;
    }
}
