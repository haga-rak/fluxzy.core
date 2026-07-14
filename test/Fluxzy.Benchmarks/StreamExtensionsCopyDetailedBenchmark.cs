using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Fluxzy.Misc.Streams;

namespace Fluxzy.Benchmarks;

[MemoryDiagnoser]
public class StreamExtensionsCopyDetailedBenchmark
{
    private byte[] _payload = null!;

    [Params(8192, 65536, 524288)]
    public int PayloadSize { get; set; }

    [Params(4096, 65536)]
    public int BufferSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _payload = new byte[PayloadSize];
        Random.Shared.NextBytes(_payload);
    }

    [Benchmark(Baseline = true)]
    public async Task<long> CopyWithFlushAfterEachWrite()
    {
        using var source = new MemoryStream(_payload);
        using var destination = new WriteOnlyStream();

        return await source.CopyDetailed(
                   destination,
                   bufferSize: BufferSize,
                   _ => { },
                   flushAfterEachWrite: true,
                   CancellationToken.None)
               .ConfigureAwait(false);
    }

    [Benchmark]
    public async Task<long> CopyWithoutFlushAfterEachWrite()
    {
        using var source = new MemoryStream(_payload);
        using var destination = new WriteOnlyStream();

        return await source.CopyDetailed(
                   destination,
                   bufferSize: BufferSize,
                   _ => { },
                   flushAfterEachWrite: false,
                   CancellationToken.None)
               .ConfigureAwait(false);
    }

    private sealed class WriteOnlyStream : Stream
    {
        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();

        public override long Position {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }
}
