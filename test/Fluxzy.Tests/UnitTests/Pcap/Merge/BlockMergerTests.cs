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
            var writer = new DummyBlockWriter(format); 

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
            var format = "000";
            var rawInput = MergeTestContentProvider.GetTestData(testCount, format: format);

            var allLines = rawInput.Split(new[] { "\r\n", "\n" },
                                       StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                   .Select(s => Encoding.UTF8.GetBytes(s.Replace(",", string.Empty)))
                                   .ToArray();

            var merger = new BlockMerger<DummyBlock, byte[]>();
            var writer = new DummyBlockWriter(format);

            var streamLimiter = new StreamLimiter(concurrentCount);

            merger.Merge(writer, s => new SleepyDummyBlockReader(streamLimiter,
                    s, format.Length), allLines);

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

    internal struct DummyBlock
    {
        public DummyBlock(ReadOnlySpan<char> rawValue)
        {
            Value = int.Parse(rawValue);
        }

        public int Value { get;  }

        public override string ToString()
        {
            return Value.ToString();
        }
    }

    internal class DummyBlockWriter : IBlockWriter<DummyBlock>
    {
        private readonly string _format;
        private readonly StringBuilder _builder = new();

        public DummyBlockWriter(string format)
        {
            _format = format;
        }

        public void Write(DummyBlock content)
        {
            if (_builder.Length != 0)
                _builder.Append(',');

            _builder.Append(content.Value.ToString(_format));
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

        private int  _nextTimeStamp = -1;

        public DummyBlockReader(string rawLine)
        {
            _fullLines = rawLine.Split(",", StringSplitOptions.RemoveEmptyEntries); 
        }

        public int NextTimeStamp {
            get
            {
                if (_nextTimeStamp != -1) {
                    return _nextTimeStamp;
                }

                if (_offset < _fullLines.Length) {
                    _nextTimeStamp = int.Parse(_fullLines[_offset]);
                    return _nextTimeStamp;
                }

                return -1;
            }
        }

        public bool Dequeue(out DummyBlock result)
        {
            _nextTimeStamp = -1;
            result = default!; 

            if (_offset >= _fullLines.Length) {
                return false;  // EOF 
            }

            var nextLine = _fullLines[_offset];

            _offset++;

            result = new DummyBlock(nextLine);

            return true; 
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

        protected override bool ReadNextBlock(SleepyStream stream, out DummyBlock result)
        {
            Span<byte> buffer = stackalloc byte[_charCount];

            var res = stream.ReadExact(buffer);

            if (!res) {
                result = default; 
                return false;
            }
            
            Span<char> charBuffer = stackalloc char[_charCount];
            
            Encoding.UTF8.GetChars(buffer, charBuffer);

            result = new DummyBlock(charBuffer);
            return true;
        }

        protected override int ReadTimeStamp(DummyBlock block)
        {
            return block.Value; 
        }
    }
}
