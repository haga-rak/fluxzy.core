// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Linq;
using System.Text;
using Fluxzy.Clients.H2.Encoder;
using Fluxzy.Clients.H2.Encoder.HPack;
using Fluxzy.Clients.H2.Encoder.Utils;
using Fluxzy.Tests._Files;
using Xunit;

namespace Fluxzy.Tests.HPack
{
    public class HPackCodecTests
    {
        [Fact]
        public void Write_And_Read_Simple_Request()
        {
            var memoryProvider = ArrayPoolMemoryProvider<char>.Default;

            var primitiveOperation = new PrimitiveOperation(new HuffmanCodec());

            var encoder = new HPackEncoder(new EncodingContext(memoryProvider),
                memoryProvider: memoryProvider, primitiveOperation: primitiveOperation);

            var decoder = new HPackDecoder(new DecodingContext(default, memoryProvider),
                memoryProvider: memoryProvider, primitiveOperation: primitiveOperation);

            Span<byte> encodingBuffer = stackalloc byte[1024 * 4];
            Span<char> decodingBuffer = stackalloc char[1024 * 4];

            var input = new UTF8Encoding(false).GetString(Headers.Req001);

            var encoded = encoder.Encode(input.AsMemory(), encodingBuffer);
            var decoded = decoder.Decode(encoded, decodingBuffer);

            var decodedString = decoded.ToString();

            var encodDin = encoder.Context.DynContent();
            var decodDin = decoder.Context.DynContent();

            Assert.Equal(input, decodedString, true);

            Assert.True(encodDin.Select(s => s.ToString().ToLowerInvariant())
                                .SequenceEqual(decodDin.Select(s => s.ToString().ToLowerInvariant())));
        }

        [Fact]
        public void Write_And_Read_Simple_Response()
        {
            var memoryProvider = ArrayPoolMemoryProvider<char>.Default;
            var primitiveOperation = new PrimitiveOperation(new HuffmanCodec());

            var encoder = new HPackEncoder(new EncodingContext(memoryProvider),
                memoryProvider: memoryProvider, primitiveOperation: primitiveOperation);

            var decoder = new HPackDecoder(new DecodingContext(default, memoryProvider),
                memoryProvider: memoryProvider, primitiveOperation: primitiveOperation);

            Span<byte> encodingBuffer = stackalloc byte[1024 * 4];
            Span<char> decodingBuffer = stackalloc char[1024 * 4];

            var input = new UTF8Encoding(false).GetString(Headers.Resp001);

            var encoded = encoder.Encode(input.AsMemory(), encodingBuffer);
            var decoded = decoder.Decode(encoded, decodingBuffer);

            var decodedString = decoded.ToString();

            var encodDin = encoder.Context.DynContent();
            var decodDin = decoder.Context.DynContent();

            Assert.Equal(input, decodedString, true);

            Assert.True(encodDin.Select(s => s.ToString().ToLowerInvariant())
                                .SequenceEqual(decodDin.Select(s => s.ToString().ToLowerInvariant())));
        }

        [Fact]
        public void Write_And_Read_Simple_Response_Double()
        {
            var memoryProvider = ArrayPoolMemoryProvider<char>.Default;
            var primitiveOperation = new PrimitiveOperation(new HuffmanCodec());

            var encoder = new HPackEncoder(new EncodingContext(memoryProvider),
                memoryProvider: memoryProvider, primitiveOperation: primitiveOperation);

            var decoder = new HPackDecoder(new DecodingContext(default, memoryProvider),
                memoryProvider: memoryProvider, primitiveOperation: primitiveOperation);

            Span<byte> encodingBuffer = stackalloc byte[1024 * 4];
            Span<char> decodingBuffer = stackalloc char[1024 * 4];

            var input = new UTF8Encoding(false).GetString(Headers.Req001);

            for (var i = 0; i < 2; i++) {
                var encoded = encoder.Encode(input.AsMemory(), encodingBuffer);
                var decoded = decoder.Decode(encoded, decodingBuffer);

                var decodedString = decoded.ToString();

                var encodDin = encoder.Context.DynContent();
                var decodDin = decoder.Context.DynContent();

                Assert.Equal(input, decodedString, true);

                Assert.True(encodDin.Select(s => s.ToString().ToLowerInvariant())
                                    .SequenceEqual(decodDin.Select(s => s.ToString().ToLowerInvariant())));
            }
        }
    }
}
