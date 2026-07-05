using System.Text;
using BenchmarkDotNet.Attributes;
using Fluxzy.Misc.Streams;

namespace Fluxzy.Benchmarks;

/// <summary>
///     Measures the HTTP/1.1 chunked body reader header parser. This exercises
///     the chunk-size line parsing path before each chunk body read.
///
///     Run: dotnet run -c Release -- --filter *ChunkedTransferReadStreamBenchmark*
/// </summary>
[MemoryDiagnoser]
public class ChunkedTransferReadStreamBenchmark
{
    private byte[] _chunkedPayload = null!;
    private byte[] _readBuffer = null!;

    [Params(10 * 1024 * 1024)]
    public int PayloadBytes { get; set; }

    [Params(1024, 16 * 1024)]
    public int ChunkBytes { get; set; }

    [Params(false, true)]
    public bool Extensions { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var payload = new byte[PayloadBytes];
        Random.Shared.NextBytes(payload);
        _chunkedPayload = BuildChunkedPayload(payload);
        _readBuffer = new byte[ChunkBytes];
    }

    [Benchmark]
    public async Task<long> ReadChunkedBodyAsync()
    {
        await using var source = new MemoryStream(_chunkedPayload, writable: false);
        await using var chunked = new ChunkedTransferReadStream(source, closeOnDone: true);

        long totalRead = 0;
        int read;

        while ((read = await chunked.ReadAsync(_readBuffer, CancellationToken.None)) > 0) {
            totalRead += read;
        }

        return totalRead;
    }

    private byte[] BuildChunkedPayload(byte[] payload)
    {
        using var destination = new MemoryStream(payload.Length + payload.Length / ChunkBytes * 16 + 16);

        for (var offset = 0; offset < payload.Length;) {
            var count = Math.Min(ChunkBytes, payload.Length - offset);
            var header = Extensions
                ? $"{count:X};foo=bar\r\n"
                : $"{count:X}\r\n";

            var headerBytes = Encoding.ASCII.GetBytes(header);
            destination.Write(headerBytes);
            destination.Write(payload, offset, count);
            destination.Write("\r\n"u8);
            offset += count;
        }

        destination.Write("0\r\n\r\n"u8);
        return destination.ToArray();
    }
}
