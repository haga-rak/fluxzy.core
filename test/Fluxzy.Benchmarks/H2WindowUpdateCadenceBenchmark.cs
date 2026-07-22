using BenchmarkDotNet.Attributes;
using Fluxzy.Clients;
using Fluxzy.Clients.H2;
using Fluxzy.Clients.H2.Encoder;
using Fluxzy.Clients.H2.Encoder.Utils;
using Fluxzy.Core;

namespace Fluxzy.Benchmarks;

/// <summary>
///     Measures how often the H2 response-body path emits WINDOW_UPDATE write tasks.
///
///     Run: dotnet run -c Release -- --filter *H2WindowUpdateCadenceBenchmark*
/// </summary>
[MemoryDiagnoser]
public class H2WindowUpdateCadenceBenchmark
{
    private const int FrameSize = 16 * 1024;

    private H2StreamSetting _setting = null!;
    private Authority _authority;
    private IHeaderEncoder _headerEncoder = null!;
    private Exchange _exchange = null!;
    private int _streamWindowUpdates;
    private int _connectionWindowUpdates;
    private int _streamUpdateBytes;
    private int _connectionUpdateBytes;

    [Params(64 * 1024, 1024 * 1024, 10 * 1024 * 1024)]
    public int ResponseBytes { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _authority = new Authority("bench.local", 443, true);
        _setting = new H2StreamSetting();

        var memoryProvider = ArrayPoolMemoryProvider<char>.Default;
        var hpackEncoder = new HPackEncoder(new EncodingContext(memoryProvider));
        var hpackDecoder = new HPackDecoder(new DecodingContext(_authority, memoryProvider));
        _headerEncoder = new HeaderEncoder(hpackEncoder, hpackDecoder, _setting);
        _exchange = MakeExchange();
    }

    [IterationSetup]
    public void IterationSetup()
    {
        _streamWindowUpdates = 0;
        _connectionWindowUpdates = 0;
        _streamUpdateBytes = 0;
        _connectionUpdateBytes = 0;
    }

    [Benchmark]
    public int ConsumeResponseBody()
    {
        using var overallWindow = new WindowSizeHolder(_setting.OverallWindowSize, 0);

        UpStreamChannel upStreamChannel = (ref WriteTask writeTask) => {
            if (writeTask.StreamIdentifier == 0) {
                _connectionWindowUpdates++;
                _connectionUpdateBytes += writeTask.WindowUpdateSize;
                return;
            }

            _streamWindowUpdates++;
            _streamUpdateBytes += writeTask.WindowUpdateSize;
        };

        var context = new StreamContext(
            connectionId: 1,
            authority: _authority,
            setting: _setting,
            headerEncoder: _headerEncoder,
            upStreamChannel: upStreamChannel,
            overallWindowSizeHolder: overallWindow);

        using var resetTokenSource = new CancellationTokenSource();
        using var streamPool = new StreamPool(context);
        using var streamWorker = new StreamWorker(1, streamPool, _exchange, resetTokenSource);

        var remaining = ResponseBytes;

        while (remaining > 0) {
            var dataSize = Math.Min(FrameSize, remaining);
            streamWorker.OnDataConsumedByCaller(dataSize);
            remaining -= dataSize;
        }

        return _streamWindowUpdates ^ _connectionWindowUpdates ^ _streamUpdateBytes ^ _connectionUpdateBytes;
    }

    [IterationCleanup]
    public void IterationCleanup()
    {
        Console.WriteLine(
            $"ResponseBytes={ResponseBytes}, StreamWindowUpdates={_streamWindowUpdates}, " +
            $"ConnectionWindowUpdates={_connectionWindowUpdates}, " +
            $"StreamUpdateBytes={_streamUpdateBytes}, ConnectionUpdateBytes={_connectionUpdateBytes}");
    }

    private Exchange MakeExchange()
    {
        var header = "GET / HTTP/2.0\r\nhost: bench.local\r\n\r\n".AsMemory();
        return new Exchange(IIdProvider.FromZero, _authority, header, "HTTP/2", DateTime.UtcNow);
    }
}
