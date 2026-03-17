// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using Fluxzy.Formatters.Producers.Grpc;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Formatters
{
    public class RawProtobufDecoderTests
    {
        [Fact]
        public void Decode_SimpleVarint()
        {
            // field 1, wire type 0 (varint), value 42
            var data = new byte[] { 0x08, 0x2A };
            var result = RawProtobufDecoder.Decode(data);

            Assert.Contains("field 1 (varint): 42", result);
        }

        [Fact]
        public void Decode_StringField()
        {
            // field 2, wire type 2 (length-delimited), value "hello"
            var data = new byte[] { 0x12, 0x05, 0x68, 0x65, 0x6C, 0x6C, 0x6F };
            var result = RawProtobufDecoder.Decode(data);

            Assert.Contains("field 2", result);
            Assert.Contains("hello", result);
        }

        [Fact]
        public void Decode_MultipleFields()
        {
            // field 1 varint 1, field 2 string "hello world!"
            var data = new byte[] {
                0x08, 0x01, // field 1 varint 1
                0x12, 0x0C, // field 2 length-delimited, 12 bytes
                0x68, 0x65, 0x6C, 0x6C, 0x6F, 0x20,
                0x77, 0x6F, 0x72, 0x6C, 0x64, 0x21 // "hello world!"
            };
            var result = RawProtobufDecoder.Decode(data);

            Assert.Contains("field 1 (varint): 1", result);
            Assert.Contains("hello world!", result);
        }

        [Fact]
        public void Decode_NestedMessage()
        {
            // field 1 varint 42, field 3 embedded { field 1 varint 1 }
            var data = new byte[] { 0x08, 0x2A, 0x1A, 0x02, 0x08, 0x01 };
            var result = RawProtobufDecoder.Decode(data);

            Assert.Contains("field 1 (varint): 42", result);
            Assert.Contains("field 3 (embedded)", result);
        }

        [Fact]
        public void Decode_EmptyInput_ReturnsEmpty()
        {
            var result = RawProtobufDecoder.Decode(ReadOnlySpan<byte>.Empty);
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void Decode_MalformedInput_DoesNotThrow()
        {
            // Just random bytes that don't form valid protobuf
            var data = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
            var result = RawProtobufDecoder.Decode(data);
            Assert.NotNull(result);
        }

        [Fact]
        public void Decode_TruncatesAtMaxLength()
        {
            // Build a larger protobuf payload
            var bytes = new List<byte>();

            for (var i = 0; i < 100; i++) {
                bytes.Add(0x08); // field 1 varint
                bytes.Add((byte) (i & 0x7F));
            }

            var result = RawProtobufDecoder.Decode(bytes.ToArray(), maxLength: 50);
            Assert.Contains("truncated", result);
        }

        [Fact]
        public void Decode_Fixed32Field()
        {
            // field 1, wire type 5 (fixed32), value
            var data = new byte[] { 0x0D, 0x01, 0x00, 0x00, 0x00 };
            var result = RawProtobufDecoder.Decode(data);

            Assert.Contains("field 1 (fixed32)", result);
        }

        [Fact]
        public void Decode_Fixed64Field()
        {
            // field 1, wire type 1 (fixed64)
            var data = new byte[] { 0x09, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            var result = RawProtobufDecoder.Decode(data);

            Assert.Contains("field 1 (fixed64)", result);
        }
    }
}
