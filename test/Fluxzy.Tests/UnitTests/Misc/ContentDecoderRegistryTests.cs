// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Fluxzy;
using Fluxzy.Extensions;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Misc
{
    /// <summary>
    ///     Covers the pluggable content-decoder feature that replaced the bundled SharpZipLib LZW support:
    ///     gzip/deflate/brotli are decoded natively, anything else is resolved through
    ///     <see cref="ContentDecoderRegistry" /> and throws when no decoder is registered.
    /// </summary>
    public class ContentDecoderRegistryTests
    {
        private const string Payload = "the quick brown fox jumps over the lazy dog";

        [Theory]
        [InlineData("gzip")]
        [InlineData("deflate")]
        [InlineData("br")]
        public void Native_encodings_decode_out_of_the_box(string token)
        {
            var compressed = Compress(token, Payload);

            Assert.Equal(Payload, DecodeToString(token, compressed));
        }

        [Fact]
        public void Empty_token_passes_the_stream_through_unchanged()
        {
            var raw = Encoding.UTF8.GetBytes(Payload);

            using var result = CompressionHelper.GetDecodedStream((string?) null, new MemoryStream(raw));
            using var reader = new StreamReader(result, Encoding.UTF8);

            Assert.Equal(Payload, reader.ReadToEnd());
        }

        [Fact]
        public void Unregistered_encoding_throws_a_clear_exception()
        {
            // No decoder registered for this token.
            var token = "x-unregistered-" + Guid.NewGuid().ToString("N");

            var ex = Assert.Throws<FluxzyException>(
                () => CompressionHelper.GetDecodedStream(token, new MemoryStream()));

            Assert.Contains(token, ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Registered_delegate_decoder_is_used()
        {
            var token = "x-deleg-" + Guid.NewGuid().ToString("N");

            try {
                // Use the gzip codec under a custom token to prove the registry is consulted.
                ContentDecoderRegistry.Register(token,
                    compressed => new GZipStream(compressed, CompressionMode.Decompress, false));

                Assert.True(ContentDecoderRegistry.Contains(token));

                var body = Compress("gzip", Payload);

                Assert.Equal(Payload, DecodeToString(token, body));
            }
            finally {
                Assert.True(ContentDecoderRegistry.Unregister(token));
                Assert.False(ContentDecoderRegistry.Contains(token));
            }
        }

        [Fact]
        public void Registered_interface_decoder_is_used()
        {
            var token = "x-iface-" + Guid.NewGuid().ToString("N");
            var decoder = new GzipBackedDecoder(token);

            try {
                ContentDecoderRegistry.Register(decoder);

                Assert.True(ContentDecoderRegistry.TryGet(token, out var resolved));
                Assert.Same(decoder, resolved);

                var body = Compress("gzip", Payload);

                Assert.Equal(Payload, DecodeToString(token, body));
            }
            finally {
                ContentDecoderRegistry.Unregister(token);
            }
        }

        [Fact]
        public void Token_lookup_is_case_insensitive()
        {
            var token = "X-Case-" + Guid.NewGuid().ToString("N");

            try {
                ContentDecoderRegistry.Register(token,
                    compressed => new GZipStream(compressed, CompressionMode.Decompress, false));

                Assert.True(ContentDecoderRegistry.Contains(token.ToLowerInvariant()));
                Assert.Equal(Payload, DecodeToString(token.ToLowerInvariant(), Compress("gzip", Payload)));
            }
            finally {
                ContentDecoderRegistry.Unregister(token);
            }
        }

        private static string DecodeToString(string token, byte[] compressed)
        {
            using var result = CompressionHelper.GetDecodedStream(token, new MemoryStream(compressed));
            using var reader = new StreamReader(result, Encoding.UTF8);

            return reader.ReadToEnd();
        }

        private static byte[] Compress(string token, string content)
        {
            var data = Encoding.UTF8.GetBytes(content);
            using var memory = new MemoryStream();

            Stream codec = token switch {
                "gzip" => new GZipStream(memory, CompressionLevel.Optimal, true),
                "deflate" => new DeflateStream(memory, CompressionLevel.Optimal, true),
                "br" => new BrotliStream(memory, CompressionLevel.Optimal, true),
                _ => throw new ArgumentOutOfRangeException(nameof(token))
            };

            using (codec) {
                codec.Write(data, 0, data.Length);
            }

            return memory.ToArray();
        }

        private sealed class GzipBackedDecoder : IContentDecoder
        {
            public GzipBackedDecoder(string encodingToken)
            {
                EncodingToken = encodingToken;
            }

            public string EncodingToken { get; }

            public Stream GetDecodedStream(Stream compressed) =>
                new GZipStream(compressed, CompressionMode.Decompress, false);
        }
    }
}
