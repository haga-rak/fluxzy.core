using System;
using System.IO;
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
    }
}
