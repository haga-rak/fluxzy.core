using System;
using System.Linq;
using Fluxzy.Clients.H2.Encoder.Utils;
using Fluxzy.Encoding.Tests.Files;
using Xunit;

namespace Fluxzy.Encoding.Tests
{
    public class Http11ParserTests
    {
        private static readonly int MaxHeaderLength = 1024 * 8; 

        [Fact]
        public void Parse_Unparse_Request_Header()
        {
            var parser = new Http11Parser(MaxHeaderLength);

            var header = Headers.Req001;
            
            Span<char> resultBuffer = stackalloc char[MaxHeaderLength];

            var headerBlocks = parser.Read(header.AsMemory(), true).ToList();
            var result = parser.Write(headerBlocks, resultBuffer).ToString();

            Assert.Equal(result, header, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void Parse_Unparse_Response_Header()
        {
            var parser = new Http11Parser(MaxHeaderLength);

            var header = Headers.Resp001;
            
            Span<char> resultBuffer = stackalloc char[MaxHeaderLength];

            var headerBlocks = parser.Read(header.AsMemory(), true).ToList();
            var result = parser.Write(headerBlocks, resultBuffer).ToString();

            Assert.Equal(result, header, StringComparer.OrdinalIgnoreCase);
        }
    }
}