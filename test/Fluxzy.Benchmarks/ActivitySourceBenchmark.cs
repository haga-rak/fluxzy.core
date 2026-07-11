using BenchmarkDotNet.Attributes;
using Fluxzy.Clients;
using Fluxzy.Core;
using Fluxzy.Logging;

namespace Fluxzy.Benchmarks;

/// <summary>
///     Measures the default ActivitySource path when no ActivityListener is attached.
///
///     Run: dotnet run -c Release -- --filter *ActivitySourceBenchmark*
/// </summary>
[MemoryDiagnoser]
public class ActivitySourceBenchmark
{
    private Exchange _exchange = null!;
    private Guid _proxyInstanceId;

    [GlobalSetup]
    public void Setup()
    {
        var authority = new Authority("example.test", 443, true);
        var requestHeader =
            "GET /api/data HTTP/1.1\r\n" +
            "Host: example.test\r\n" +
            "User-Agent: benchmark\r\n" +
            "Accept: application/json\r\n" +
            "\r\n";

        _exchange = new Exchange(
            IIdProvider.FromZero,
            authority,
            requestHeader.AsMemory(),
            "HTTP/1.1",
            DateTime.UtcNow);
        _proxyInstanceId = Guid.NewGuid();
    }

    [Benchmark]
    public object? StartExchangeActivity_NoListener()
    {
        return FluxzyActivitySource.StartExchangeActivity(_exchange, _proxyInstanceId);
    }
}
