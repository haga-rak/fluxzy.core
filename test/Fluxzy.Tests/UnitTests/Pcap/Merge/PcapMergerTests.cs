using System;
using System.Linq;
using System.Text;
using Fluxzy.Core.Pcap.Pcapng;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Pcap.Merge
{
    public class PcapMergerTests
    {
        [Theory]
        [InlineData(20)]
        [InlineData(200)]
        public void Validate_Merge(int testCount)
        {
            var rawInput = MergeTestContentProvider.GetTestData(testCount); 

            var allLines = rawInput.Split(new[] { "\r\n", "\n" },
                                       StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                   .ToList();

            var merger = new PcapMerger<DummyBlock, string>();
            var writer = new DummyBlockWriter(); 

            merger.Merge(writer, (s) => new DummyBlockReader(s), allLines.ToArray());

            var result = writer.GetRawLine();

            var expectedResult = string.Join(",", 
                Enumerable.Range(0, testCount).Select(i => i.ToString("0000")));

            Assert.Equal(expectedResult, result);
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
        private int offset = 0; 

        private int ? _nextTimeStamp = null;

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

                if (offset < _fullLines.Length) {
                    _nextTimeStamp = int.Parse(_fullLines[offset]);
                    return _nextTimeStamp;
                }

                return null;
            }
        }

        public DummyBlock? Dequeue()
        {
            _nextTimeStamp = null;

            if (offset >= _fullLines.Length) {
                return null;  // EOF 
            }

            var nextLine = _fullLines[offset];

            offset++;

            return new DummyBlock(nextLine);
        }
    }
}
