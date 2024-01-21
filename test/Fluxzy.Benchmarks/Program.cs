using BenchmarkDotNet.Running;
using Fluxzy.Benchmarks.Pcap;

namespace Fluxzy.Benchmarks;

internal static class Program
{
    public static void Main(string[] args)
    {
        // var summary = BenchmarkRunner.Run<Md5VsSha256>();
        var summary = BenchmarkRunner.Run<BlockMergeBenchmark>();
    }
}