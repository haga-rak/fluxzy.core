using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Fluxzy.Core.Pcap.Pcapng;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Pcap.Merge
{
    public class BlockMergerTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(20)]
        [InlineData(200)]
        [InlineData(381)]
        public void Validate_Merge(int testCount)
        {
            var format = "00000";
            var rawInput = MergeTestContentProvider.GetTestData(testCount, format: format); 

            var allLines = rawInput.Split(new[] { "\r\n", "\n" },
                                       StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                   .ToList();

            var merger = new BlockMerger<DummyBlock, string>();
            var writer = new DummyBlockWriter(); 

            merger.Merge(writer, s => new DummyBlockReader(s), allLines.ToArray());

            var result = writer.GetRawLine();

            var expectedResult = string.Join(",", 
                Enumerable.Range(0, testCount).Select(i => i.ToString(format)));

            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [MemberData(nameof(GetTestData))]
        public void Validate_Sleepy_Merge(int testCount, int concurrentCount)
        {
            var format = "00000";
            var rawInput = MergeTestContentProvider.GetTestData(testCount, format: format); 

            var allLines = rawInput.Split(new[] { "\r\n", "\n" },
                                       StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                   .ToList();

            var merger = new BlockMerger<DummyBlock, string>();
            var writer = new DummyBlockWriter(); 

            var streamLimiter = new StreamLimiter(concurrentCount);

            merger.Merge(writer, s => new SleepyDummyBlockReader(streamLimiter,
                Encoding.UTF8.GetBytes(s.Replace(",", string.Empty)), format.Length), allLines.ToArray());

            var result = writer.GetRawLine();

            var expectedResult = string.Join(",", 
                Enumerable.Range(0, testCount).Select(i => i.ToString(format)));

            Assert.Equal(expectedResult, result);
        }

        public static IEnumerable<object[]> GetTestData()
        {
            var testCounts = new[] { 0, 1, 20, 200, 381 };
            var concurrentCount = new[] { 1, 2, 3, 9 };

            foreach (var testCount in testCounts) {
                foreach (var count in concurrentCount) {
                    yield return new object[] { testCount, count };
                }
            }
        }
    }

    internal class DummyBlock
    {
        public DummyBlock(string rawValue)
        {
            Value = int.Parse(rawValue);
            RawValue = rawValue;
        }

        public int Value { get;  }

        public string RawValue { get; }

        public override string ToString()
        {
            return RawValue;
        }
    }

    internal class DummyBlockWriter : IBlockWriter<DummyBlock>
    {
        private readonly StringBuilder _builder = new();

        public void Write(DummyBlock content)
        {
            if (_builder.Length != 0)
                _builder.Append(',');

            _builder.Append(content.RawValue);
        }

        public string GetRawLine()
        {
            return _builder.ToString();
        }
    }

    internal class DummyBlockReader : IBlockReader<DummyBlock>
    {
        private readonly string[] _fullLines;
        private int _offset; 

        private int ? _nextTimeStamp;

        public DummyBlockReader(string rawLine)
        {
            _fullLines = rawLine.Split(",", StringSplitOptions.RemoveEmptyEntries); 
        }

        public int? NextTimeStamp {
            get
            {
                if (_nextTimeStamp != null) {
                    return _nextTimeStamp;
                }

                if (_offset < _fullLines.Length) {
                    _nextTimeStamp = int.Parse(_fullLines[_offset]);
                    return _nextTimeStamp;
                }

                return null;
            }
        }

        public DummyBlock? Dequeue()
        {
            _nextTimeStamp = null;

            if (_offset >= _fullLines.Length) {
                return null;  // EOF 
            }

            var nextLine = _fullLines[_offset];

            _offset++;

            return new DummyBlock(nextLine);
        }

        public void Sleep()
        {

        }

        public void Dispose()
        {
        }
    }

    internal class SleepyDummyBlockReader : SleepyStreamBlockReader<DummyBlock>
    {
        private readonly int _charCount;

        public SleepyDummyBlockReader(StreamLimiter streamLimiter, byte[] data, int charCount)
            : base(streamLimiter, () => new MemoryStream(data))
        {
            _charCount = charCount;
        }

        protected override DummyBlock? ReadNextBlock(SleepyStream stream)
        {
            Span<byte> buffer = stackalloc byte[_charCount];

            var res = stream.ReadExact(buffer);

            if (!res) {
                return null;
            }

            return new DummyBlock(Encoding.UTF8.GetString(buffer));
        }

        protected override int ReadTimeStamp(DummyBlock block)
        {
            return block.Value; 
        }
    }

}
