using BenchmarkDotNet.Attributes;
using Fluxzy.Clients.H2.Encoder.Utils;

namespace Fluxzy.Benchmarks;

/// <summary>
///     Measures production H2/H1 header-name set lookups used while translating headers.
///
///     Run: dotnet run -c Release -- --filter *HeaderNameLookupBenchmark*
/// </summary>
[MemoryDiagnoser]
public class HeaderNameLookupBenchmark
{
    private readonly ReadOnlyMemory<char>[] _headers = {
        "connection".AsMemory(),
        "keep-alive".AsMemory(),
        "proxy-authenticate".AsMemory(),
        "trailer".AsMemory(),
        "upgrade".AsMemory(),
        "alt-svc".AsMemory(),
        "expect".AsMemory(),
        "x-fluxzy-live-edit".AsMemory(),
        "content-type".AsMemory(),
        "user-agent".AsMemory(),
        "accept".AsMemory(),
        "cookie".AsMemory(),
        "x-custom-header".AsMemory(),
    };

    [Benchmark]
    public int NonForwardableLookup()
    {
        var count = 0;

        foreach (var header in _headers) {
            if (Http11Constants.IsNonForwardableHeader(header))
                count++;
        }

        return count;
    }

    [Benchmark]
    public int HopByHopLookup()
    {
        var count = 0;

        foreach (var header in _headers) {
            if (Http11Constants.IsH1HopByHopHeader(header))
                count++;
        }

        return count;
    }
}
