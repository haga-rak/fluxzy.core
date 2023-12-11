// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Text;
using Fluxzy.Clients.H2.Encoder;
using Xunit;

namespace Fluxzy.Tests.UnitTests.HPack
{
    public class HuffmanCodecTests
    {
        [Fact]
        public void Encoding_Decoding_With_Many_Inputs()
        {
            var r = new Random(9);

            var testCount = 100_000;

            var minLength = 20;
            var maxLength = 40;

            var testBuffer = new byte[maxLength];
            Span<byte> bufferEncoded = stackalloc byte[2048];
            Span<byte> bufferDecoded = stackalloc byte[2048];

            var codec = new HuffmanCodec();

            for (var i = 0; i < testCount; i++)
            {
                var input = StringGenerationHelper.GenerateRandomInput(testBuffer,
                    minLength, maxLength, r);

                if (i < 5)
                    continue;

                var provisionalLength = codec.GetEncodedLength(input.Span);

                var encoded = codec.Encode(input.Span, bufferEncoded);
                var decoded = codec.Decode(encoded, bufferDecoded);

                Assert.Equal(Encoding.ASCII.GetString(input.Span),
                    Encoding.ASCII.GetString(decoded));

                Assert.Equal(encoded.Length,
                    provisionalLength);
            }
        }

        [Fact]
        public void Encoding_Decoding_With_Simple_Input()
        {
            var testCount = 100_000;

            Span<byte> bufferEncoded = stackalloc byte[2048];
            Span<byte> bufferDecoded = stackalloc byte[2048];

            var codec = new HuffmanCodec();

            var input = StringGenerationHelper.GetFastString();

            for (var i = 0; i < testCount; i++)
            {
                var provisionalLength = codec.GetEncodedLength(input.Span);
                var encoded = codec.Encode(input.Span, bufferEncoded);
                var decoded = codec.Decode(encoded, bufferDecoded);


                Assert.Equal(Encoding.ASCII.GetString(input.Span),
                    Encoding.ASCII.GetString(decoded));

                Assert.Equal(encoded.Length,
                    provisionalLength);
            }
        }

        [Fact]
        public void Encoding_Decoding_EmptyString()
        {
            var inputString = "";

            Span<byte> bufferEncoded = stackalloc byte[2048];
            Span<byte> bufferDecoded = stackalloc byte[2048];

            var codec = new HuffmanCodec();

            var input = new Memory<byte>(Encoding.ASCII.GetBytes(inputString));

            var encoded = codec.Encode(input.Span, bufferEncoded);
            var decoded = codec.Decode(encoded, bufferDecoded);

            Assert.Equal(Encoding.ASCII.GetString(input.Span),
                Encoding.ASCII.GetString(decoded));
        }

        [Fact]
        public void Encoding_Decoding_WithSpecificString()
        {
            Span<byte> encoded = stackalloc byte[] { 185, 88, 211, 63, 255 };

            Span<byte> bufferDecoded = stackalloc byte[2048];

            var codec = new HuffmanCodec();

            var decoded = codec.Decode(encoded, bufferDecoded);
        }
    }

    public static class StringGenerationHelper
    {
        private static readonly byte[] FastString = Encoding.ASCII.GetBytes("ABCDEFJHIJKLMNOPQRSTUVWXYZ");

        public static Memory<byte> GenerateRandomInput(byte[] buffer, int min, int max, Random random)
        {
            var size = random.Next(min, max);
            random.NextBytes(new Span<byte>(buffer, 0, size));

            return new Memory<byte>(buffer, 0, size);
        }

        public static Memory<byte> GetFastString()
        {
            return new Memory<byte>(FastString, 0, FastString.Length);
        }
    }
}
