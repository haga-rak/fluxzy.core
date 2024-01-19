// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Text;
using BenchmarkDotNet.Attributes;
using Fluxzy.Core.Pcap.Pcapng;
using Fluxzy.Tests.UnitTests.Pcap.Merge;

namespace Fluxzy.Benchmarks.Pcap
{
    [MemoryDiagnoser(true)]
    public class BlockMergeBenchmark
    {
        private static readonly string Format = "0000";
        private readonly int _formatLength = Format.Length;
        private readonly int _concurrentCount = 50;
        private BlockMerger<DummyBlock,byte[]> _merger = null!;
        private DoNothingWritter _writer = null!;
        private byte[][] _allLines = null!;
        private StreamLimiter _streamLimiter = null!;

        [GlobalSetup]
        public void Setup()
        {
            var rawInput = MergeTestContentProvider
                .GetTestData(2000, format: Format);

            _allLines = rawInput.Split(new[] { "\r\n", "\n" },
                                       StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                   .Select(s => Encoding.UTF8.GetBytes(s.Replace(",", string.Empty)))
                                   .ToArray();

            _merger = new BlockMerger<DummyBlock, byte[]>();
            _writer = new DoNothingWritter();
        }

        [IterationSetup]
        public void CreateLimiter()
        {
            _streamLimiter = new StreamLimiter(_concurrentCount);
        }
        
        [Benchmark]
        public void MergeBlock()
        {
            _merger.Merge(_writer, BlockFactory, _allLines);
        }

        private IBlockReader<DummyBlock> BlockFactory(byte[] s)
        {
            return new SleepyDummyBlockReader(_streamLimiter, s, _formatLength);
        }
    }

    internal class DoNothingWritter : IBlockWriter<DummyBlock>
    {
        public void Write(ref DummyBlock content)
        {
        }
    }
}
