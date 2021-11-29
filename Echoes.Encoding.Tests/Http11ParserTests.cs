using System;
using System.Linq;
using Echoes.Encoding.Tests.Files;
using Echoes.Encoding.Utils;
using Xunit;

namespace Echoes.Encoding.Tests
{
    public class Http11ParserTests
    {
        private static readonly int MaxHeaderLength = 1024 * 8; 

        [Fact]
        public void Parse_Unparse_Request_Header()
        {
            using var memoryProvider = new ArrayPoolMemoryProvider<char>();
            var parser = new Http11Parser(MaxHeaderLength, memoryProvider);

            var header = Headers.Req001;
            
            Span<char> resultBuffer = stackalloc char[MaxHeaderLength];

            var headerBlocks = parser.Read(header.AsMemory(), true).ToList();
            var result = parser.Write(headerBlocks, resultBuffer).ToString();

            Assert.Equal(result, header, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void Parse_Unparse_Response_Header()
        {
            using var memoryProvider = new ArrayPoolMemoryProvider<char>();
            var parser = new Http11Parser(MaxHeaderLength, memoryProvider);

            var header = Headers.Resp001;
            
            Span<char> resultBuffer = stackalloc char[MaxHeaderLength];

            var headerBlocks = parser.Read(header.AsMemory(), true).ToList();
            var result = parser.Write(headerBlocks, resultBuffer).ToString();

            Assert.Equal(result, header, StringComparer.OrdinalIgnoreCase);
        }
    }
}