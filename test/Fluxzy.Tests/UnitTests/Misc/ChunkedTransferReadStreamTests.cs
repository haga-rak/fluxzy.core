// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.IO;
using System.Text;
using System;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Misc.Streams;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Misc
{
    public class ChunkedTransferReadStreamTests
    {
        [Fact]
        public async Task ReadAsync_ParsesChunkExtensionsAndTrailers()
        {
            await using var source = new MemoryStream(
                Encoding.ASCII.GetBytes("5;foo=bar\r\nhello\r\n0\r\nx-one: 1\r\n\r\n"));
            await using var chunked = new ChunkedTransferReadStream(source, closeOnDone: false);
            var buffer = new byte[8];

            await chunked.ReadExactlyAsync(buffer.AsMemory(0, 5), CancellationToken.None);

            Assert.Equal("hello", Encoding.ASCII.GetString(buffer, 0, 5));
            Assert.Equal(0, await chunked.ReadAsync(buffer, CancellationToken.None));
            Assert.NotNull(chunked.Trailers);
            Assert.Single(chunked.Trailers!);
            Assert.Equal("x-one", chunked.Trailers![0].Name.ToString());
            Assert.Equal("1", chunked.Trailers[0].Value.ToString());
        }

        [Fact]
        public void Read_ParsesChunkExtensionsAndTrailers()
        {
            using var source = new MemoryStream(
                Encoding.ASCII.GetBytes("A;foo=bar\r\nhellohello\r\n0\r\nx-one: 1\r\n\r\n"));
            using var chunked = new ChunkedTransferReadStream(source, closeOnDone: false);
            var buffer = new byte[16];

            var read = chunked.Read(buffer, 0, buffer.Length);

            Assert.Equal(10, read);
            Assert.Equal("hellohello", Encoding.ASCII.GetString(buffer, 0, read));
            Assert.Equal(0, chunked.Read(buffer, 0, buffer.Length));
            Assert.NotNull(chunked.Trailers);
            Assert.Single(chunked.Trailers!);
            Assert.Equal("x-one", chunked.Trailers![0].Name.ToString());
            Assert.Equal("1", chunked.Trailers[0].Value.ToString());
        }

        [Fact]
        public async Task ReadAsync_InvalidChunkSize_ThrowsIOException()
        {
            await using var source = new MemoryStream(Encoding.ASCII.GetBytes("Z\r\nhello\r\n0\r\n\r\n"));
            await using var chunked = new ChunkedTransferReadStream(source, closeOnDone: true);
            var buffer = new byte[8];

            await Assert.ThrowsAsync<IOException>(async () =>
                await chunked.ReadExactlyAsync(buffer.AsMemory(0, 1), CancellationToken.None));
        }

        [Theory]
        [InlineData("a\r\nhellohello\r\n0\r\n\r\n", "hellohello")]
        [InlineData("A\r\nhellohello\r\n0\r\n\r\n", "hellohello")]
        [InlineData("000000000000000A\r\nhellohello\r\n0\r\n\r\n", "hellohello")]
        public async Task ReadAsync_ParsesValidHexChunkSizes(string payload, string expected)
        {
            await using var source = new MemoryStream(Encoding.ASCII.GetBytes(payload));
            await using var chunked = new ChunkedTransferReadStream(source, closeOnDone: true);
            var buffer = new byte[expected.Length];

            await chunked.ReadExactlyAsync(buffer, CancellationToken.None);

            Assert.Equal(expected, Encoding.ASCII.GetString(buffer));
        }

        [Theory]
        [InlineData("\r\nhello\r\n0\r\n\r\n")]
        [InlineData(" 5\r\nhello\r\n0\r\n\r\n")]
        [InlineData("5 \r\nhello\r\n0\r\n\r\n")]
        [InlineData("8000000000000000\r\nhello\r\n0\r\n\r\n")]
        [InlineData("10000000000000000\r\nhello\r\n0\r\n\r\n")]
        [InlineData("5\rhello\r\n0\r\n\r\n")]
        public async Task ReadAsync_InvalidChunkSizeLines_Throw(string payload)
        {
            await using var source = new MemoryStream(Encoding.ASCII.GetBytes(payload));
            await using var chunked = new ChunkedTransferReadStream(source, closeOnDone: true);
            var buffer = new byte[8];

            await Assert.ThrowsAsync<IOException>(async () =>
                await chunked.ReadExactlyAsync(buffer.AsMemory(0, 1), CancellationToken.None));
        }

        [Theory]
        [InlineData("a\r\nhellohello\r\n0\r\n\r\n", "hellohello")]
        [InlineData("A\r\nhellohello\r\n0\r\n\r\n", "hellohello")]
        [InlineData("000000000000000A\r\nhellohello\r\n0\r\n\r\n", "hellohello")]
        public void Read_ParsesValidHexChunkSizes(string payload, string expected)
        {
            using var source = new MemoryStream(Encoding.ASCII.GetBytes(payload));
            using var chunked = new ChunkedTransferReadStream(source, closeOnDone: true);
            var buffer = new byte[expected.Length];

            var read = chunked.Read(buffer, 0, buffer.Length);

            Assert.Equal(expected.Length, read);
            Assert.Equal(expected, Encoding.ASCII.GetString(buffer));
        }

        [Theory]
        [InlineData("\r\nhello\r\n0\r\n\r\n")]
        [InlineData(" 5\r\nhello\r\n0\r\n\r\n")]
        [InlineData("5 \r\nhello\r\n0\r\n\r\n")]
        [InlineData("8000000000000000\r\nhello\r\n0\r\n\r\n")]
        [InlineData("10000000000000000\r\nhello\r\n0\r\n\r\n")]
        [InlineData("5\rhello\r\n0\r\n\r\n")]
        public void Read_InvalidChunkSizeLines_Throw(string payload)
        {
            using var source = new MemoryStream(Encoding.ASCII.GetBytes(payload));
            using var chunked = new ChunkedTransferReadStream(source, closeOnDone: true);
            var buffer = new byte[8];

            Assert.Throws<IOException>(() => chunked.Read(buffer, 0, 1));
        }
    }
}
