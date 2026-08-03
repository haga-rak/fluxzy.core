using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Misc.Streams;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Misc
{
    public class ChunkedTransferWriteStreamTests
    {
        [Fact]
        public async Task SequentialWrites_EmitValidChunkFrames()
        {
            await using var destination = new MemoryStream();
            await using var chunked = new ChunkedTransferWriteStream(destination);

            chunked.Write("hello"u8.ToArray());
            await chunked.WriteAsync(new byte[16], CancellationToken.None);
            await chunked.WriteEof();

            var expected = "5\r\nhello\r\n10\r\n" + new string('\0', 16) + "\r\n0\r\n\r\n";
            Assert.Equal(expected, Encoding.ASCII.GetString(destination.ToArray()));
        }

        [Fact]
        public async Task WriteAsync_StagesChunkIntoOneInnerWrite()
        {
            await using var destination = new CountingWriteStream();
            await using var chunked = new ChunkedTransferWriteStream(destination);

            await chunked.WriteAsync(new byte[16 * 1024]);

            Assert.Equal(1, destination.AsyncWrites);
            Assert.Equal(16 * 1024 + "4000\r\n\r\n"u8.Length, destination.Length);
        }

        [Theory]
        [InlineData(64 * 1024, 1)]
        [InlineData(64 * 1024 + 1, 3)]
        public async Task StagingBoundary_UsesExpectedInnerWriteCount(int payloadLength, int expectedWrites)
        {
            await using var destination = new CountingWriteStream();
            await using var chunked = new ChunkedTransferWriteStream(destination);

            await chunked.WriteAsync(new byte[payloadLength]);

            Assert.Equal(expectedWrites, destination.AsyncWrites);
        }

        [Fact]
        public void Write_StagesChunkIntoOneInnerWrite()
        {
            using var destination = new CountingWriteStream();
            using var chunked = new ChunkedTransferWriteStream(destination);

            chunked.Write(new byte[16 * 1024], 0, 16 * 1024);

            Assert.Equal(1, destination.SyncWrites);
            Assert.Equal(16 * 1024 + "4000\r\n\r\n"u8.Length, destination.Length);
        }

        [Fact]
        public void WriteSpan_StagesValidChunkIntoOneInnerWrite()
        {
            using var destination = new CountingWriteStream();
            using var chunked = new ChunkedTransferWriteStream(destination);

            chunked.Write("span"u8);

            Assert.Equal(1, destination.SyncWrites);
            Assert.Equal("4\r\nspan\r\n"u8.ToArray(), destination.ToArray());
        }

        [Fact]
        public async Task ReusedBuffer_DoesNotLeakPreviousHeader()
        {
            await using var destination = new MemoryStream();
            await using var chunked = new ChunkedTransferWriteStream(destination);

            await chunked.WriteAsync(new byte[0x10000]);
            await chunked.WriteAsync("x"u8.ToArray());
            await chunked.WriteEof();

            var bytes = destination.ToArray();
            var secondHeaderOffset = "10000\r\n"u8.Length + 0x10000 + 2;
            Assert.Equal("1\r\nx\r\n0\r\n\r\n"u8.ToArray(), bytes[secondHeaderOffset..]);
        }

        [Fact]
        public async Task EmptyWrite_DoesNotEmitFinalChunk()
        {
            await using var destination = new MemoryStream();
            await using var chunked = new ChunkedTransferWriteStream(destination);

            chunked.Write(Array.Empty<byte>(), 0, 0);
            chunked.Write(ReadOnlySpan<byte>.Empty);
            await chunked.WriteAsync(Array.Empty<byte>(), 0, 0, CancellationToken.None);
            await chunked.WriteAsync(ReadOnlyMemory<byte>.Empty);
            await chunked.WriteEof();

            Assert.Equal("0\r\n\r\n"u8.ToArray(), destination.ToArray());
        }

        [Fact]
        public void EmptyArrayWrite_StillValidatesArguments()
        {
            using var destination = new CountingWriteStream();
            using var chunked = new ChunkedTransferWriteStream(destination);

            Assert.Throws<ArgumentNullException>(() => chunked.Write(null!, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => chunked.Write(Array.Empty<byte>(), -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => chunked.Write(Array.Empty<byte>(), 0, -1));
            Assert.ThrowsAny<ArgumentException>(() => chunked.Write(Array.Empty<byte>(), 0, 1));
            Assert.Equal(0, destination.SyncWrites);
        }

        [Fact]
        public async Task EmptyAsyncWrites_HonorAlreadyCancelledToken()
        {
            await using var destination = new CountingWriteStream();
            await using var chunked = new ChunkedTransferWriteStream(destination);
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => chunked.WriteAsync(Array.Empty<byte>(), 0, 0, cancellation.Token));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                async () => await chunked.WriteAsync(ReadOnlyMemory<byte>.Empty, cancellation.Token));
            Assert.Equal(0, destination.AsyncWrites);
        }

        private sealed class CountingWriteStream : MemoryStream
        {
            public int AsyncWrites { get; private set; }

            public int SyncWrites { get; private set; }

            public override void Write(byte[] buffer, int offset, int count)
            {
                SyncWrites++;
                base.Write(buffer, offset, count);
            }

            public override ValueTask WriteAsync(
                ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            {
                AsyncWrites++;
                return base.WriteAsync(buffer, cancellationToken);
            }
        }
    }
}
