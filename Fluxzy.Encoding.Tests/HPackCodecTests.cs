using System;
using System.Linq;
using Echoes.Clients.H2.Encoder;
using Echoes.Clients.H2.Encoder.HPack;
using Echoes.Clients.H2.Encoder.Utils;
using Fluxzy.Encoding.Tests.Files;
using Xunit;

namespace Echoes.Encoding.Tests
{
    public class HPackCodecTests
    {
        [Fact]
        public void Write_And_Read_Simple_Request()
        {
            ArrayPoolMemoryProvider<char> memoryProvider = ArrayPoolMemoryProvider<char>.Default;
            Http11Parser parser = new Http11Parser(4096);
            PrimitiveOperation primitiveOperation = new PrimitiveOperation(new HuffmanCodec());

            var encoder = new HPackEncoder(new EncodingContext(memoryProvider), 
                memoryProvider : memoryProvider, parser: parser, primitiveOperation : primitiveOperation);
            var decoder = new HPackDecoder(new DecodingContext(default, memoryProvider),
                memoryProvider : memoryProvider, parser: parser, primitiveOperation: primitiveOperation);

            Span<byte> encodingBuffer = stackalloc byte[1024 * 4];
            Span<char> decodingBuffer = stackalloc char[1024 * 4];

            var input = Headers.Req001;

            var encoded = encoder.Encode(input.AsMemory(), encodingBuffer);
            var decoded = decoder.Decode(encoded, decodingBuffer);

            var decodedString = decoded.ToString();

            var encodDin = encoder.Context.DynContent();
            var decodDin = decoder.Context.DynContent();

            Assert.Equal(input, decodedString, true);
            Assert.True(encodDin.Select(s => s.ToString().ToLowerInvariant()).SequenceEqual(decodDin.Select(s => s.ToString().ToLowerInvariant())));
        }

        [Fact]
        public void Write_And_Read_Simple_Response()
        {
            var memoryProvider = ArrayPoolMemoryProvider<char>.Default;
            var parser = new Http11Parser(4096);
            var primitiveOperation = new PrimitiveOperation(new HuffmanCodec());

            var encoder = new HPackEncoder(new EncodingContext(memoryProvider), 
                memoryProvider : memoryProvider, parser: parser, primitiveOperation : primitiveOperation);
            var decoder = new HPackDecoder(new DecodingContext(default, memoryProvider),
                memoryProvider : memoryProvider, parser: parser, primitiveOperation: primitiveOperation);

            Span<byte> encodingBuffer = stackalloc byte[1024 * 4];
            Span<char> decodingBuffer = stackalloc char[1024 * 4];

            var input = Headers.Resp001;

            var encoded = encoder.Encode(input.AsMemory(), encodingBuffer);
            var decoded = decoder.Decode(encoded, decodingBuffer);

            var decodedString = decoded.ToString();

            var encodDin = encoder.Context.DynContent();
            var decodDin = decoder.Context.DynContent();


            Assert.Equal(input, decodedString, true);
            Assert.True(encodDin.Select(s => s.ToString().ToLowerInvariant()).SequenceEqual(decodDin.Select(s => s.ToString().ToLowerInvariant())));
        }

        [Fact]
        public void Write_And_Read_Simple_Response_Double()
        {
            var memoryProvider = ArrayPoolMemoryProvider<char>.Default;
            var parser = new Http11Parser(4096);
            var primitiveOperation = new PrimitiveOperation(new HuffmanCodec());

            var encoder = new HPackEncoder(new EncodingContext(memoryProvider), 
                memoryProvider : memoryProvider, parser: parser, primitiveOperation : primitiveOperation);
            var decoder = new HPackDecoder(new DecodingContext(default, memoryProvider),
                memoryProvider : memoryProvider, parser: parser, primitiveOperation: primitiveOperation);

            Span<byte> encodingBuffer = stackalloc byte[1024 * 4];
            Span<char> decodingBuffer = stackalloc char[1024 * 4];

            var input = Headers.Req001;

            for (int i = 0; i < 2; i++)
            {
                var encoded = encoder.Encode(input.AsMemory(), encodingBuffer);
                var decoded = decoder.Decode(encoded, decodingBuffer);

                var decodedString = decoded.ToString();

                var encodDin = encoder.Context.DynContent();
                var decodDin = decoder.Context.DynContent();

                Assert.Equal(input, decodedString, true);
                Assert.True(encodDin.Select(s => s.ToString().ToLowerInvariant()).SequenceEqual(decodDin.Select(s => s.ToString().ToLowerInvariant())));

            }
        }
        
    }
}