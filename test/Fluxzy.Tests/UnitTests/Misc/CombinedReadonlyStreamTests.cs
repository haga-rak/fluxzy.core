using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Misc.Streams;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Misc
{
    public class CombinedReadonlyStreamTests
    {
        [Fact]
        public async Task ReadAsync_MemoryOverload_PreservesPrefixAndPartialInnerReads()
        {
            var prefix = Encoding.ASCII.GetBytes("prefix-");
            using var inner = new AsyncMemoryReadStream(
                Encoding.ASCII.GetBytes("inner"), maximumReadSize: 2);
            await using var stream = new CombinedReadonlyStream(false, prefix, inner);
            var buffer = new byte[16];

            var prefixRead = await stream.ReadAsync(buffer.AsMemory());
            Assert.Equal(prefix.Length, prefixRead);
            Assert.Equal("prefix-", Encoding.ASCII.GetString(buffer, 0, prefixRead));
            Assert.Equal(prefix.Length, stream.Position);

            var firstInnerRead = await stream.ReadAsync(buffer.AsMemory());
            Assert.Equal(2, firstInnerRead);
            Assert.Equal("in", Encoding.ASCII.GetString(buffer, 0, firstInnerRead));

            var secondInnerRead = await stream.ReadAsync(buffer.AsMemory());
            Assert.Equal(2, secondInnerRead);
            Assert.Equal("ne", Encoding.ASCII.GetString(buffer, 0, secondInnerRead));

            var finalInnerRead = await stream.ReadAsync(buffer.AsMemory());
            Assert.Equal(1, finalInnerRead);
            Assert.Equal("r", Encoding.ASCII.GetString(buffer, 0, finalInnerRead));

            Assert.Equal(0, await stream.ReadAsync(buffer.AsMemory()));
            Assert.Equal(prefix.Length + 5, stream.Position);
            Assert.Equal(4, inner.MemoryReadCount);
        }

        [Fact]
        public async Task ReadAsync_ArrayOverload_DelegatesToMemoryOverload()
        {
            using var inner = new AsyncMemoryReadStream(Encoding.ASCII.GetBytes("inner"));
            await using var stream = new CombinedReadonlyStream(false, inner);
            var buffer = new byte[8];

            var read = await stream.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None);

            Assert.Equal(5, read);
            Assert.Equal("inner", Encoding.ASCII.GetString(buffer, 0, read));
            Assert.Equal(1, inner.MemoryReadCount);
        }

        [Fact]
        public async Task ReadAsync_ForwardsCancellationAfterPooledPrefix()
        {
            var prefix = Encoding.ASCII.GetBytes("prefix");
            using var inner = new AsyncMemoryReadStream(Encoding.ASCII.GetBytes("inner"));
            await using var stream = new CombinedReadonlyStream(false, prefix, inner);
            var buffer = new byte[16];
            using var cancellation = new CancellationTokenSource();

            cancellation.Cancel();
            Assert.Equal(prefix.Length, await stream.ReadAsync(buffer.AsMemory(), cancellation.Token));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                async () => await stream.ReadAsync(buffer.AsMemory(), cancellation.Token));

            Assert.Equal(prefix.Length, stream.Position);
            Assert.Equal(0, inner.MemoryReadCount);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ReadAsync_EofHonorsInnerStreamOwnership(bool closeStreams)
        {
            var inner = new AsyncMemoryReadStream(Encoding.ASCII.GetBytes("inner"));
            await using (var stream = new CombinedReadonlyStream(closeStreams, inner)) {
                var buffer = new byte[8];

                Assert.Equal(5, await stream.ReadAsync(buffer.AsMemory()));
                Assert.Equal(0, await stream.ReadAsync(buffer.AsMemory()));
                Assert.Equal(closeStreams, inner.IsDisposed);
            }

            Assert.Equal(closeStreams, inner.IsDisposed);
            inner.Dispose();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task DisposeAsync_HonorsInnerStreamOwnership(bool closeStreams)
        {
            var inner = new AsyncMemoryReadStream(Encoding.ASCII.GetBytes("inner"), maximumReadSize: 1);
            var stream = new CombinedReadonlyStream(closeStreams, inner);

            Assert.Equal(1, await stream.ReadAsync(new byte[1].AsMemory()));
            await stream.DisposeAsync();

            Assert.Equal(closeStreams, inner.IsDisposed);
            inner.Dispose();
        }

        private sealed class AsyncMemoryReadStream : Stream
        {
            private readonly byte[] _content;
            private readonly int _maximumReadSize;
            private int _position;

            public AsyncMemoryReadStream(byte[] content, int maximumReadSize = int.MaxValue)
            {
                _content = content;
                _maximumReadSize = maximumReadSize;
            }

            public bool IsDisposed { get; private set; }

            public int MemoryReadCount { get; private set; }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position {
                get => _position;
                set => throw new NotSupportedException();
            }

            public override async ValueTask<int> ReadAsync(
                Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                MemoryReadCount++;
                await Task.Yield();

                var read = Math.Min(Math.Min(buffer.Length, _maximumReadSize), _content.Length - _position);
                _content.AsMemory(_position, read).CopyTo(buffer);
                _position += read;

                return read;
            }

            public override Task<int> ReadAsync(
                byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                throw new InvalidOperationException("The array ReadAsync overload must not be used.");
            }

            public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            public override void Flush() => throw new NotSupportedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

            protected override void Dispose(bool disposing)
            {
                if (disposing) {
                    IsDisposed = true;
                }

                base.Dispose(disposing);
            }
        }
    }
}
