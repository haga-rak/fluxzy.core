using System.Buffers;
using BenchmarkDotNet.Attributes;
using Fluxzy.Clients.H2;

namespace Fluxzy.Benchmarks;

/// <summary>
///     Measures the H2ConnectionPool single non-WINDOW_UPDATE writer path.
///     The optimized method calls the same internal helper used by the runtime
///     write loop; the baseline mirrors the previous rent/copy/write behavior
///     used even when only one frame was queued.
///
///     Run: dotnet run -c Release -- --filter *H2ConnectionPoolWriterBatchBenchmark*
/// </summary>
[MemoryDiagnoser]
public class H2ConnectionPoolWriterBatchBenchmark
{
    private byte[][] _frames = null!;

    [Params(9, 1024, 16 * 1024)]
    public int FrameBytes { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _frames = new byte[1][];

        for (var i = 0; i < _frames.Length; i++) {
            var frame = new byte[FrameBytes];
            Random.Shared.NextBytes(frame);
            _frames[i] = frame;
        }
    }

    [Benchmark(Baseline = true)]
    public async Task<long> PreviousRentCopyBatch()
    {
        var writeTask = BuildTask();
        var totalSize = writeTask.BufferBytes.Length;
        await using var stream = new CountingSinkStream();

        var batchBuffer = ArrayPool<byte>.Shared.Rent(totalSize);

        try {
            var offset = 0;

            writeTask.BufferBytes.Span.CopyTo(batchBuffer.AsSpan(offset));

            await stream.WriteAsync(batchBuffer.AsMemory(0, totalSize)).ConfigureAwait(false);
            writeTask.OnComplete(null);
        }
        finally {
            ArrayPool<byte>.Shared.Return(batchBuffer);
        }

        return stream.BytesWritten;
    }

    [Benchmark]
    public async Task<long> OptimizedRuntimeHelper()
    {
        var writeTask = BuildTask();
        await using var stream = new CountingSinkStream();

        await H2ConnectionPool.WriteSingleNonWindowUpdateAsync(
            writeTask, stream, CancellationToken.None).ConfigureAwait(false);

        return stream.BytesWritten;
    }

    private WriteTask BuildTask()
    {
        return new WriteTask(H2FrameType.Headers, streamIdentifier: 1,
            priority: 0, streamDependency: 0, _frames[0]);
    }

    private sealed class CountingSinkStream : Stream
    {
        public long BytesWritten { get; private set; }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => BytesWritten;
        public override long Position { get => BytesWritten; set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => BytesWritten += count;

        public override ValueTask WriteAsync(
            ReadOnlyMemory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            BytesWritten += buffer.Length;
            return default;
        }
    }
}
