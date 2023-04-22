// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Linq;
using System.Text;
using Fluxzy.Clients.H2.Encoder.Utils;
using Fluxzy.Tests._Files;
using Xunit;

namespace Fluxzy.Tests.HPack
{
    public class Http11ParserTests
    {
        private static readonly int MaxHeaderLength = 1024 * 8;

        [Fact]
        public void Parse_Unparse_Request_Header()
        {
            var header = new UTF8Encoding(false).GetString(Headers.Req001);

            Span<char> resultBuffer = stackalloc char[MaxHeaderLength];

            var headerBlocks = Http11Parser.Read(header.AsMemory()).ToList();
            var result = Http11Parser.Write(headerBlocks, resultBuffer).ToString();

            Assert.Equal(header, result, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void Parse_Unparse_Response_Header()
        {
            var header = new UTF8Encoding(false).GetString(Headers.Resp001);

            Span<char> resultBuffer = stackalloc char[MaxHeaderLength];

            var headerBlocks = Http11Parser.Read(header.AsMemory()).ToList();
            var result = Http11Parser.Write(headerBlocks, resultBuffer).ToString();

            Assert.Equal(header, result, StringComparer.OrdinalIgnoreCase);
        }
    }
}
