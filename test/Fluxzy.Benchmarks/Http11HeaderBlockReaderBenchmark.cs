using BenchmarkDotNet.Attributes;
using Fluxzy.Clients.H11;
using Fluxzy.Misc.ResizableBuffers;

namespace Fluxzy.Benchmarks;

/// <summary>
///     Measures production HTTP/1.1 header block scanning.
///
///     Run: dotnet run -c Release -- --filter *Http11HeaderBlockReaderBenchmark*
/// </summary>
[MemoryDiagnoser]
public class Http11HeaderBlockReaderBenchmark
{
    private byte[] _headerBlock = null!;

    [Params(1, 16, 1024)]
    public int MaxReadBytes { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _headerBlock = System.Text.Encoding.ASCII.GetBytes(
            "HTTP/1.1 200 OK\r\n" +
            "Server: benchmark\r\n" +
            "Content-Type: application/json\r\n" +
            "Content-Length: 2\r\n" +
            "Connection: keep-alive\r\n" +
            "\r\n{}");
    }

    [Benchmark]
    public async ValueTask<int> ReadHeaderBlock()
    {
        using var stream = new FragmentedReadStream(_headerBlock, MaxReadBytes);
        using var buffer = RsBuffer.Allocate(1024);

        var result = await Http11HeaderBlockReader.GetNext(
            stream,
            buffer,
            firstByteReceived: null,
            headerBlockReceived: null,
            throwOnError: true,
            token: CancellationToken.None,
            dontThrowIfEarlyClosed: false);

        return result.HeaderLength;
    }

    private sealed class FragmentedReadStream : Stream
    {
        private readonly byte[] _buffer;
        private readonly int _maxReadBytes;
        private int _offset;

        public FragmentedReadStream(byte[] buffer, int maxReadBytes)
        {
            _buffer = buffer;
            _maxReadBytes = maxReadBytes;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _buffer.Length;
        public override long Position { get => _offset; set => throw new NotSupportedException(); }

        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            if (_offset >= _buffer.Length)
                return new ValueTask<int>(0);

            var count = Math.Min(Math.Min(buffer.Length, _maxReadBytes), _buffer.Length - _offset);
            _buffer.AsMemory(_offset, count).CopyTo(buffer);
            _offset += count;
            return new ValueTask<int>(count);
        }
    }
}
