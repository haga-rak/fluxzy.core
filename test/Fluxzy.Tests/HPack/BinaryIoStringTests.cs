// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using Fluxzy.Clients.H2.Encoder;
using Fluxzy.Clients.H2.Encoder.HPack;
using Xunit;

namespace Fluxzy.Tests.HPack
{
    public class BinaryIoStringTests
    {
        [Fact]
        public void Read_Write_Simple_String()
        {
            var binaryHelper = new PrimitiveOperation(new HuffmanCodec());

            var expectedText =
                "!\"#$%\r\n\t&\\'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";

            var input = expectedText.AsSpan();

            var buffer = (Span<byte>) stackalloc byte[2048];
            var bufferDecoded = (Span<char>) stackalloc char[2048];

            var encoded = binaryHelper.WriteString(input, buffer, false);
            var decoded = binaryHelper.ReadString(encoded, bufferDecoded, out _);

            var decodedString = decoded.ToString();

            Assert.Equal(expectedText, decodedString);
        }

        [Fact]
        public void Read_Write_Empty_String()
        {
            var binaryHelper = new PrimitiveOperation(new HuffmanCodec());
            var expectedText = string.Empty;

            var input = expectedText.AsSpan();

            var buffer = (Span<byte>) stackalloc byte[2048];
            var bufferDecoded = (Span<char>) stackalloc char[2048];

            var encoded = binaryHelper.WriteString(input, buffer, false);
            var decoded = binaryHelper.ReadString(encoded, bufferDecoded, out _);
            var decodedString = decoded.ToString();

            Assert.Equal(expectedText, decodedString);
        }
    }
}
