using BenchmarkDotNet.Attributes;
using Fluxzy.Clients;
using Fluxzy.Core;
using Fluxzy.Rules.Filters;
using Fluxzy.Rules.Filters.RequestFilters;
using Fluxzy.Rules.Filters.ResponseFilters;

namespace Fluxzy.Benchmarks;

/// <summary>
///     Measures common header/auth rule filters against live exchanges.
///
///     Run: dotnet run -c Release -- --filter *RuleHeaderFilterBenchmark*
/// </summary>
[MemoryDiagnoser]
public class RuleHeaderFilterBenchmark
{
    private Authority _authority;
    private ExchangeContext _context = null!;
    private Exchange _exchange = null!;
    private Filter _filter = null!;

    public enum HeaderFilterScenario
    {
        HasAuthorization,
        HasBearer,
        RequestHeaderContains,
        RequestHeaderRegexCapture,
        ResponseHeaderExact
    }

    [Params(HeaderFilterScenario.HasAuthorization, HeaderFilterScenario.HasBearer,
        HeaderFilterScenario.RequestHeaderContains, HeaderFilterScenario.RequestHeaderRegexCapture,
        HeaderFilterScenario.ResponseHeaderExact)]
    public HeaderFilterScenario Scenario { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _authority = new Authority("api.example.test", 443, true);
        _context = new BenchmarkExchangeContextBuilder().Create(_authority, true).AsTask().GetAwaiter().GetResult();
        _exchange = Exchange.CreateUntrackedExchange(
            IIdProvider.FromZero,
            _context,
            _authority,
            (
                "GET /api/data HTTP/1.1\r\n" +
                "Host: api.example.test\r\n" +
                "User-Agent: Mozilla/5.0 benchmark Chrome/120\r\n" +
                "Accept: application/json\r\n" +
                "Authorization: Bearer token\r\n" +
                "\r\n").AsMemory(),
            Stream.Null,
            (
                "HTTP/1.1 200 OK\r\n" +
                "Content-Type: application/json\r\n" +
                "Cache-Control: no-store\r\n" +
                "Content-Length: 0\r\n" +
                "\r\n").AsMemory(),
            Stream.Null,
            isSecure: true,
            httpVersion: "HTTP/1.1",
            receivedFromProxy: DateTime.UtcNow);

        _filter = Scenario switch {
            HeaderFilterScenario.HasAuthorization => new HasAuthorizationFilter(),
            HeaderFilterScenario.HasBearer => new HasAuthorizationBearerFilter(),
            HeaderFilterScenario.RequestHeaderContains => new RequestHeaderFilter("Chrome/120", StringSelectorOperation.Contains, "User-Agent"),
            HeaderFilterScenario.RequestHeaderRegexCapture => new RequestHeaderFilter(@"Chrome/(?<version>\d+)", StringSelectorOperation.Regex, "User-Agent"),
            HeaderFilterScenario.ResponseHeaderExact => new ResponseHeaderFilter("application/json", StringSelectorOperation.Exact, "Content-Type"),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    [Benchmark]
    public bool Apply()
    {
        return _filter.Apply(_context, _authority, _exchange, null);
    }
}
