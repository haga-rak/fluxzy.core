using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Misc.Streams;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Misc
{
    public class StreamExtensionsCopyDetailedTests
    {
        [Fact]
        public async Task CopyDetailed_BufferOverload_DefaultsToFlushAfterEachWrite()
        {
            await VerifyFlushBehavior(
                copy: (source, destination, onCopied) =>
                    source.CopyDetailed(destination, new byte[4], onCopied, CancellationToken.None),
                expectedFlushCount: 2);
        }

        [Fact]
        public async Task CopyDetailed_BufferOverload_AllowsDisablingFlushAfterEachWrite()
        {
            await VerifyFlushBehavior(
                copy: (source, destination, onCopied) =>
                    source.CopyDetailed(
                        destination,
                        new byte[4],
                        onCopied,
                        flushAfterEachWrite: false,
                        CancellationToken.None),
                expectedFlushCount: 0);
        }

        [Fact]
        public async Task CopyDetailed_BufferSizeOverload_AllowsDisablingFlushAfterEachWrite()
        {
            await VerifyFlushBehavior(
                copy: (source, destination, onCopied) =>
                    source.CopyDetailed(
                        destination,
                        bufferSize: 4,
                        onCopied,
                        flushAfterEachWrite: false,
                        CancellationToken.None),
                expectedFlushCount: 0);
        }

        [Fact]
        public async Task CopyDetailed_BufferOverload_UsesMemoryAsyncVirtuals()
        {
            var payload = Encoding.ASCII.GetBytes("memory-only");
            using var source = new MemoryOnlyReadStream(payload, maximumReadSize: 3);
            using var destination = new MemoryOnlyWriteStream();
            var copiedBytes = 0;

            var totalCopied = await source.CopyDetailed(
                destination,
                new byte[4],
                copied => copiedBytes += copied,
                flushAfterEachWrite: false,
                CancellationToken.None);

            Assert.Equal(payload.Length, totalCopied);
            Assert.Equal(payload.Length, copiedBytes);
            Assert.Equal(payload, destination.ToArray());
            Assert.True(source.MemoryReadCount > 0);
            Assert.True(destination.MemoryWriteCount > 0);
        }

        [Fact]
        public async Task CopyDetailed_UsesMemoryAsyncVirtualsThroughHttp11StreamWrappers()
        {
            var prefix = Encoding.ASCII.GetBytes("pre-");
            var payload = Encoding.ASCII.GetBytes("payload");
            using var inner = new MemoryOnlyReadStream(payload, maximumReadSize: 2);
            using var pushback = new PushbackReadStream(inner);
            pushback.Push(prefix);
            using var bounded = new ContentBoundStream(pushback, prefix.Length + payload.Length);
            using var dispatched = new MemoryOnlyWriteStream();
            using var dispatch = new DispatchStream(bounded, closeOnDone: false, dispatched);
            using var source = new RecomposedStream(dispatch, Stream.Null);
            using var destination = new MemoryOnlyWriteStream();

            var totalCopied = await source.CopyDetailed(
                destination,
                new byte[3],
                _ => { },
                flushAfterEachWrite: false,
                CancellationToken.None);

            var expected = Encoding.ASCII.GetBytes("pre-payload");
            Assert.Equal(expected.Length, totalCopied);
            Assert.Equal(expected, destination.ToArray());
            Assert.Equal(expected, dispatched.ToArray());
            Assert.Equal(expected.Length, bounded.TotalRead);
            Assert.True(inner.MemoryReadCount > 0);
        }

        private static async Task VerifyFlushBehavior(
            Func<Stream, FlushCountingStream, Action<int>, ValueTask<long>> copy,
            int expectedFlushCount)
        {
            using var source = new MemoryStream(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
            using var destination = new FlushCountingStream();
            var copiedBytes = 0;

            var totalCopied = await copy(source, destination, copied => copiedBytes += copied).ConfigureAwait(false);

            Assert.Equal(8, totalCopied);
            Assert.Equal(8, copiedBytes);
            Assert.Equal(8, destination.TotalWritten);
            Assert.Equal(expectedFlushCount, destination.FlushCount);
        }

        private sealed class FlushCountingStream : Stream
        {
            public int FlushCount { get; private set; }

            public int TotalWritten { get; private set; }

            public override bool CanRead => false;

            public override bool CanSeek => false;

            public override bool CanWrite => true;

            public override long Length => TotalWritten;

            public override long Position {
                get => TotalWritten;
                set => throw new NotSupportedException();
            }

            public override void Flush()
            {
                FlushCount++;
            }

            public override Task FlushAsync(CancellationToken cancellationToken)
            {
                FlushCount++;
                return Task.CompletedTask;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                TotalWritten += count;
            }

            public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            {
                TotalWritten += buffer.Length;
                return ValueTask.CompletedTask;
            }
        }

        private sealed class MemoryOnlyReadStream : Stream
        {
            private readonly byte[] _content;
            private readonly int _maximumReadSize;
            private int _position;

            public MemoryOnlyReadStream(byte[] content, int maximumReadSize)
            {
                _content = content;
                _maximumReadSize = maximumReadSize;
            }

            public int MemoryReadCount { get; private set; }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position {
                get => _position;
                set => throw new NotSupportedException();
            }

            public override ValueTask<int> ReadAsync(
                Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                MemoryReadCount++;

                var read = Math.Min(Math.Min(buffer.Length, _maximumReadSize), _content.Length - _position);
                _content.AsMemory(_position, read).CopyTo(buffer);
                _position += read;

                return new ValueTask<int>(read);
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
        }

        private sealed class MemoryOnlyWriteStream : Stream
        {
            private readonly MemoryStream _content = new();

            public int MemoryWriteCount { get; private set; }

            public byte[] ToArray() => _content.ToArray();

            public override bool CanRead => false;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public override long Length => _content.Length;
            public override long Position {
                get => _content.Position;
                set => throw new NotSupportedException();
            }

            public override ValueTask WriteAsync(
                ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                MemoryWriteCount++;
                _content.Write(buffer.Span);

                return ValueTask.CompletedTask;
            }

            public override Task WriteAsync(
                byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                throw new InvalidOperationException("The array WriteAsync overload must not be used.");
            }

            public override void Flush()
            {
            }

            public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

            protected override void Dispose(bool disposing)
            {
                if (disposing) {
                    _content.Dispose();
                }

                base.Dispose(disposing);
            }
        }
    }
}
