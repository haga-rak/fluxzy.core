using System;
using BenchmarkDotNet.Attributes;
using Fluxzy.Clients.H2;
using Fluxzy.Clients.H2.Encoder;
using Fluxzy.Clients.H2.Encoder.Utils;
using Fluxzy.Core;
using Fluxzy.Misc.ResizableBuffers;

namespace Fluxzy.Benchmarks;

/// <summary>
///     Measures HeaderEncoder.Encode() — the per-request path that turns an
///     HTTP/1.1 header block into HPACK-encoded HEADERS frame(s).
///     Runs on the H2 WriteLoop for every forwarded request.
///
///     Run: dotnet run -c Release -- --filter *HeaderEncoder*
/// </summary>
[MemoryDiagnoser]
public class HeaderEncoderBenchmark
{
    // ~40 B — minimal GET.
    private const string SmallHeaders =
        "GET / HTTP/1.1\r\n" +
        "Host: example.com\r\n" +
        "\r\n";

    // ~400 B — typical API request.
    private const string TypicalHeaders =
        "GET /api/users/42 HTTP/1.1\r\n" +
        "Host: api.example.com\r\n" +
        "User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36\r\n" +
        "Accept: application/json, text/plain, */*\r\n" +
        "Accept-Language: en-US,en;q=0.9\r\n" +
        "Accept-Encoding: gzip, deflate, br\r\n" +
        "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.payload.sig\r\n" +
        "Referer: https://www.example.com/\r\n" +
        "Connection: keep-alive\r\n" +
        "\r\n";

    // ~1.2 kB — heavy browser navigation with cookies and sec-ch-ua.
    private const string LargeHeaders =
        "GET /complex/path/with/segments?a=1&b=2 HTTP/1.1\r\n" +
        "Host: www.example.com\r\n" +
        "User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36\r\n" +
        "Accept: text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8\r\n" +
        "Accept-Language: en-US,en;q=0.9,fr;q=0.8\r\n" +
        "Accept-Encoding: gzip, deflate, br, zstd\r\n" +
        "Cookie: session=abcd1234efgh5678ijkl; csrftoken=xyzpdq7890abcdef; theme=dark; lang=en-US; trackingId=9f8e7d6c5b4a3928; utm_source=google; utm_campaign=spring_sale; _ga=GA1.2.123456789.1700000000; _gid=GA1.2.987654321.1700000000\r\n" +
        "Referer: https://www.google.com/search?q=example+query&oq=example+query\r\n" +
        "Sec-Ch-Ua: \"Not_A Brand\";v=\"8\", \"Chromium\";v=\"120\", \"Google Chrome\";v=\"120\"\r\n" +
        "Sec-Ch-Ua-Mobile: ?0\r\n" +
        "Sec-Ch-Ua-Platform: \"Windows\"\r\n" +
        "Sec-Fetch-Dest: document\r\n" +
        "Sec-Fetch-Mode: navigate\r\n" +
        "Sec-Fetch-Site: cross-site\r\n" +
        "Sec-Fetch-User: ?1\r\n" +
        "Upgrade-Insecure-Requests: 1\r\n" +
        "Cache-Control: max-age=0\r\n" +
        "Connection: keep-alive\r\n" +
        "\r\n";

    public enum HeaderPayload
    {
        Small,
        Typical,
        Large
    }

    private HeaderEncoder _encoder = null!;
    private RsBuffer _destination = null!;
    private ReadOnlyMemory<char> _payload;
    private int _streamId;

    [Params(HeaderPayload.Small, HeaderPayload.Typical, HeaderPayload.Large)]
    public HeaderPayload Payload { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var setting = new H2StreamSetting();
        var memoryProvider = ArrayPoolMemoryProvider<char>.Default;
        var hPackEncoder = new HPackEncoder(new EncodingContext(memoryProvider));
        var hPackDecoder = new HPackDecoder(
            new DecodingContext(new Authority("bench.local", 443, true), memoryProvider));

        _encoder = new HeaderEncoder(hPackEncoder, hPackDecoder, setting);
        _destination = RsBuffer.Allocate(16 * 1024);

        _payload = Payload switch {
            HeaderPayload.Small => SmallHeaders.AsMemory(),
            HeaderPayload.Typical => TypicalHeaders.AsMemory(),
            HeaderPayload.Large => LargeHeaders.AsMemory(),
            _ => throw new InvalidOperationException()
        };

        // Prime the HPACK dynamic table so we measure steady-state encoding
        // (long-lived H2 connections reuse the same encoder across many requests).
        var priming = new HeaderEncodingJob(_payload, 1, 0);
        _encoder.Encode(priming, _destination, endStream: true);
        _streamId = 3;
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _destination.Dispose();
    }

    [Benchmark]
    public int Encode()
    {
        var job = new HeaderEncodingJob(_payload, _streamId, 0);
        _streamId += 2;
        var result = _encoder.Encode(job, _destination, endStream: true);
        return result.Length;
    }
}
