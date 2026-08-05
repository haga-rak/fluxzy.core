using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Clients.H11;
using Fluxzy.Clients.H2.Encoder;
using Fluxzy.Core;
using Fluxzy.Misc.Streams;
using Xunit;
using ResizableBuffer = Fluxzy.Misc.ResizableBuffers.RsBuffer;

namespace Fluxzy.Tests.UnitTests.Core
{
    public class Http11ChunkedWriterOwnershipTests
    {
        [Fact]
        public async Task RequestSourceFailure_ReturnsChunkBufferWithoutClosingTransport()
        {
            var expected = new IOException("source read failed");
            var source = new SegmentThenTerminalStream("request"u8.ToArray(), expected);
            var destination = new TrackingMemoryStream();
            var pool = new TrackingArrayPool();
            var exchange = CreateChunkedRequest(source, destination);
            var processor = CreateProcessor(pool);
            using var buffer = ResizableBuffer.Allocate(1024);
            using var scope = new ExchangeScope();

            var actual = await Assert.ThrowsAsync<IOException>(
                () => processor.Process(exchange, buffer, scope, CancellationToken.None).AsTask());

            Assert.Same(expected, actual);
            Assert.Equal(1, pool.RentCount);
            Assert.Equal(1, pool.ReturnCount);
            Assert.Equal(0, destination.DisposeCount);
        }

        [Fact]
        public async Task RequestCancellation_ReturnsChunkBufferWithoutClosingTransport()
        {
            var source = new SegmentThenTerminalStream("request"u8.ToArray());
            var destination = new TrackingMemoryStream();
            var pool = new TrackingArrayPool();
            var exchange = CreateChunkedRequest(source, destination);
            var processor = CreateProcessor(pool);
            using var buffer = ResizableBuffer.Allocate(1024);
            using var scope = new ExchangeScope();
            using var cancellation = new CancellationTokenSource();

            var processing = processor.Process(exchange, buffer, scope, cancellation.Token).AsTask();
            await source.WaitingForTerminalRead.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.Equal(0, pool.ReturnCount);

            cancellation.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await processing);
            Assert.Equal(1, pool.RentCount);
            Assert.Equal(1, pool.ReturnCount);
            Assert.Equal(0, destination.DisposeCount);
        }

        [Fact]
        public async Task DelayedResponseWriteFailure_ReturnsBufferOnlyAfterWriteCompletes()
        {
            var expected = new IOException("destination write failed");
            var destination = new DelayedFailureStream();
            var pool = new TrackingArrayPool();
            var pipe = CreateDownStreamPipe(destination, pool);
            using var buffer = ResizableBuffer.Allocate(1024);

            var writing = pipe.WriteResponseBody(
                new MemoryStream("response"u8.ToArray()), buffer, true, 0, new Response(),
                CancellationToken.None).AsTask();
            await destination.WriteStarted.WaitAsync(TimeSpan.FromSeconds(2));

            Assert.Equal(1, pool.RentCount);
            Assert.Equal(0, pool.ReturnCount);
            Assert.Equal(0, destination.DisposeCount);

            destination.Fail(expected);
            var actual = await Assert.ThrowsAsync<IOException>(async () => await writing);

            Assert.Same(expected, actual);
            Assert.Equal(1, pool.ReturnCount);
            Assert.Equal(0, destination.DisposeCount);

            pipe.Dispose();
            Assert.Equal(1, destination.DisposeCount);
        }

        [Fact]
        public async Task SuccessfulResponseEofAndTrailers_ReturnBufferExactlyOnce()
        {
            var destination = new TrackingMemoryStream();
            var pool = new TrackingArrayPool();
            var pipe = CreateDownStreamPipe(destination, pool);
            using var buffer = ResizableBuffer.Allocate(1024);
            var response = new Response {
                Trailers = new List<HeaderField> { new("x-check", "yes") }
            };

            await pipe.WriteResponseBody(
                new MemoryStream("response"u8.ToArray()), buffer, true, 0, response,
                CancellationToken.None);

            Assert.Equal(
                "8\r\nresponse\r\n0\r\nx-check: yes\r\n\r\n"u8.ToArray(),
                destination.ToArray());
            Assert.Equal(1, pool.RentCount);
            Assert.Equal(1, pool.ReturnCount);
            Assert.Equal(0, destination.DisposeCount);

            pipe.Dispose();
            Assert.Equal(1, pool.ReturnCount);
            Assert.Equal(1, destination.DisposeCount);
        }

        [Fact]
        public async Task BufferReturn_ClearsEntireStagedHighWaterRangeExactlyOnce()
        {
            var destination = new MemoryStream();
            var pool = new TrackingArrayPool(fill: 0xA5);
            var chunked = new ChunkedTransferWriteStream(destination, pool);
            var largePayload = new byte[4096];
            Array.Fill(largePayload, (byte) 0x5A);

            await chunked.WriteAsync(largePayload);
            await chunked.WriteAsync("x"u8.ToArray());
            await chunked.WriteEof();
            chunked.Dispose();

            const int largestFrameLength = 6 + 4096 + 2;
            Assert.Equal(1, pool.ReturnCount);
            Assert.All(pool.Buffer.AsSpan(0, largestFrameLength).ToArray(), value => Assert.Equal(0, value));
            Assert.Equal(0xA5, pool.Buffer[largestFrameLength]);
        }

        private static Http11PoolProcessing CreateProcessor(ArrayPool<byte> pool)
            => new(
                TimeSpan.Zero,
                Timeout.InfiniteTimeSpan,
                Timeout.InfiniteTimeSpan,
                chunkBufferPool: pool);

        private static Exchange CreateChunkedRequest(Stream source, Stream destination)
        {
            var authority = new Authority("test.local", 80, false);
            var exchange = new Exchange(
                IIdProvider.FromZero,
                authority,
                "POST / HTTP/1.1\r\nHost: test.local\r\nTransfer-Encoding: chunked\r\n\r\n".AsMemory(),
                "HTTP/1.1",
                DateTime.UtcNow) {
                Connection = new Connection(authority, IIdProvider.FromZero) {
                    ReadStream = Stream.Null,
                    WriteStream = destination
                }
            };
            exchange.Request.Body = source;
            return exchange;
        }

        private static Http11DownStreamPipe CreateDownStreamPipe(Stream destination, ArrayPool<byte> pool)
            => new(
                IIdProvider.FromZero,
                new Authority("test.local", 80, false),
                Stream.Null,
                destination,
                contextBuilder: null!,
                chunkBufferPool: pool);

        private sealed class TrackingArrayPool : ArrayPool<byte>
        {
            public TrackingArrayPool(byte fill = 0)
            {
                Buffer = new byte[128 * 1024];
                Array.Fill(Buffer, fill);
            }

            public byte[] Buffer { get; }

            public int RentCount { get; private set; }

            public int ReturnCount { get; private set; }

            public override byte[] Rent(int minimumLength)
            {
                if (minimumLength > Buffer.Length)
                    throw new InvalidOperationException("Test pool buffer is too small.");

                RentCount++;
                return Buffer;
            }

            public override void Return(byte[] array, bool clearArray = false)
            {
                if (!ReferenceEquals(Buffer, array))
                    throw new InvalidOperationException("Unexpected buffer returned.");

                ReturnCount++;
            }
        }

        private sealed class TrackingMemoryStream : MemoryStream
        {
            public int DisposeCount { get; private set; }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                    DisposeCount++;

                base.Dispose(disposing);
            }
        }

        private sealed class DelayedFailureStream : Stream
        {
            private readonly TaskCompletionSource _writeStarted =
                new(TaskCreationOptions.RunContinuationsAsynchronously);
            private readonly TaskCompletionSource _writeCompletion =
                new(TaskCreationOptions.RunContinuationsAsynchronously);

            public Task WriteStarted => _writeStarted.Task;

            public int DisposeCount { get; private set; }

            public void Fail(Exception exception) => _writeCompletion.SetException(exception);

            public override ValueTask WriteAsync(
                ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            {
                _writeStarted.TrySetResult();
                return new ValueTask(_writeCompletion.Task);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                    DisposeCount++;

                base.Dispose(disposing);
            }

            public override bool CanRead => false;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public override long Length => throw new NotSupportedException();
            public override long Position {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }
            public override void Flush() { }
            public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }

        private sealed class SegmentThenTerminalStream : Stream
        {
            private readonly byte[] _segment;
            private readonly Exception? _terminalException;
            private readonly TaskCompletionSource _waitingForTerminalRead =
                new(TaskCreationOptions.RunContinuationsAsynchronously);
            private bool _segmentRead;

            public SegmentThenTerminalStream(byte[] segment, Exception? terminalException = null)
            {
                _segment = segment;
                _terminalException = terminalException;
            }

            public Task WaitingForTerminalRead => _waitingForTerminalRead.Task;

            public override ValueTask<int> ReadAsync(
                Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!_segmentRead) {
                    _segmentRead = true;
                    _segment.CopyTo(buffer);
                    return ValueTask.FromResult(_segment.Length);
                }

                _waitingForTerminalRead.TrySetResult();
                return _terminalException != null
                    ? ValueTask.FromException<int>(_terminalException)
                    : WaitForCancellation(cancellationToken);
            }

            private static async ValueTask<int> WaitForCancellation(CancellationToken cancellationToken)
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return 0;
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }
            public override void Flush() { }
            public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }
    }
}
