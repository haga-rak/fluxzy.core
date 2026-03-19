using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Fluxzy.Core;

namespace Fluxzy.Benchmarks;

/// <summary>
///     Compares direct stream write (H1.1 path) vs channel-based write indirection (H2 path).
///
///     H1.1: read source → write to output → flush (direct)
///     H2:   rent buffer → read source → enqueue PooledFrame → WriteLoop reads channel → coalesce → write
///
///     Run: dotnet run -c Release -- --filter *ChannelWritePipe*
/// </summary>
[MemoryDiagnoser]
public class ChannelWritePipeBenchmark
{
    private byte[] _sourceData = null!;
    private MemoryStream _source = null!;
    private MemoryStream _destination = null!;
    private byte[] _buffer = null!;

    [Params(0, 8192, 65536)]
    public int PayloadSize { get; set; }

    [IterationSetup]
    public void IterationSetup()
    {
        _sourceData = new byte[PayloadSize];
        Random.Shared.NextBytes(_sourceData);
        _source = new MemoryStream(_sourceData);
        _destination = new MemoryStream(PayloadSize + 1024);
        _buffer = new byte[16384]; // H2 default max frame size
    }

    [IterationCleanup]
    public void IterationCleanup()
    {
        _source.Dispose();
        _destination.Dispose();
    }

    /// <summary>
    ///     Baseline: direct read → write → flush (CopyDetailed / H1.1 style).
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task<long> DirectStreamWrite()
    {
        _source.Position = 0;
        _destination.Position = 0;

        int read;

        while ((read = await _source.ReadAsync(_buffer.AsMemory())) > 0) {
            await _destination.WriteAsync(_buffer.AsMemory(0, read));
        }

        await _destination.FlushAsync();

        return _destination.Position;
    }

    /// <summary>
    ///     H2 path: rent per-frame buffer → read → enqueue PooledFrame → WriteLoop → write.
    ///     Simulates the channel indirection without actual H2 framing overhead.
    /// </summary>
    [Benchmark]
    public async Task<long> ChannelCoalescedWrite()
    {
        _source.Position = 0;
        _destination.Position = 0;

        var channel = Channel.CreateUnbounded<PooledFrame>();
        var cts = new CancellationTokenSource();

        // Writer task: read source, enqueue frames (simulates stream worker)
        var writerTask = Task.Run(async () => {
            int read;

            while ((read = await _source.ReadAsync(new byte[16384].AsMemory())) > 0) {
                // Simulate per-frame ArrayPool rent
                var frameBuffer = ArrayPool<byte>.Shared.Rent(read);
                _sourceData.AsSpan(0, read).CopyTo(frameBuffer);
                channel.Writer.TryWrite(new PooledFrame(frameBuffer, read, pooled: true));
            }

            channel.Writer.Complete();
        });

        // Reader task: WriteLoop — dequeue and write to destination (simulates H2 write loop)
        var readerTask = Task.Run(async () => {
            await foreach (var frame in channel.Reader.ReadAllAsync()) {
                await _destination.WriteAsync(frame.Array.AsMemory(0, frame.Length));

                if (frame.Pooled)
                    ArrayPool<byte>.Shared.Return(frame.Array);
            }

            await _destination.FlushAsync();
        });

        await Task.WhenAll(writerTask, readerTask);
        cts.Dispose();

        return _destination.Position;
    }
}
