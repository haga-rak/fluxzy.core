// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Clients.H2;
using Fluxzy.Clients.H2.Encoder;
using Fluxzy.Clients.H2.Encoder.Utils;
using Fluxzy.Clients.H2.Frames;
using Fluxzy.Core;
using Fluxzy.Misc.Streams;
using Fluxzy.Tests._Fixtures;
using Xunit;
using ResizableBuffer = Fluxzy.Misc.ResizableBuffers.RsBuffer;

namespace Fluxzy.Tests.UnitTests.H2Client
{
    public class StreamWorkerUploadTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task BodylessRequestSkipsUploadBufferAndTask(bool nonSeekableBody)
        {
            using var pipe = new DuplexPipe();
            var authority = new Authority("test.local", 443, true);
            var setting = new H2StreamSetting();
            var body = nonSeekableBody ? new NonSeekableEmptyStream() : null;

            await using var pool = new H2ConnectionPool(
                new RecomposedStream(pipe.ClientReadStream, pipe.ClientWriteStream),
                setting,
                authority,
                new Connection(authority, new TestIdProvider()),
                _ => { });
            var requestBodyWorkStarted = 0;
            pool.RequestBodyWorkStartedForTests = () => requestBodyWorkStarted++;
            pool.Init();

            await NegotiateMaxFrameSize(pipe, 128 * 1024);

            var exchange = MakeBodylessExchange(authority, body);
            using var sharedBuffer = ResizableBuffer.Allocate(4096);
            var headersReceived = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var requestHeaders = ReadRequestHeadersAsync(pipe.ServerReadStream, headersReceived);
            var response = WriteResponseHeadersAsync(pipe.ServerWriteStream, headersReceived.Task);

            await Task.WhenAll(
                pool.Send(exchange, null!, sharedBuffer, null!, CancellationToken.None).AsTask(),
                response).WaitAsync(TimeSpan.FromSeconds(5));
            await WriteResponseEndStreamAsync(pipe.ServerWriteStream);

            Assert.True((await requestHeaders).Flags.HasFlag(HeaderFlags.EndStream));
            Assert.Equal(0, requestBodyWorkStarted);
            Assert.Equal(0, body?.ReadCount ?? 0);
            Assert.Equal(exchange.Metrics.RequestHeaderSent, exchange.Metrics.RequestBodySent);
        }

        [Theory]
        [InlineData(32 * 1024, 32 * 1024)]
        [InlineData(128 * 1024, 64 * 1024)]
        public async Task UploadUsesNegotiatedFrameSizeAnd64KiBPayloadCap(
            int remoteMaxFrameSize, int expectedPayloadSize)
        {
            var payload = CreatePayload(expectedPayloadSize * 2 + 123);
            using var pipe = new DuplexPipe();
            var authority = new Authority("test.local", 443, true);
            var setting = new H2StreamSetting {
                OverallWindowSize = payload.Length + remoteMaxFrameSize
            };
            setting.Remote.WindowSize = payload.Length + remoteMaxFrameSize;

            await using var pool = new H2ConnectionPool(
                new RecomposedStream(pipe.ClientReadStream, pipe.ClientWriteStream),
                setting,
                authority,
                new Connection(authority, new TestIdProvider()),
                _ => { });
            pool.Init();

            await NegotiateMaxFrameSize(pipe, remoteMaxFrameSize);

            var exchange = MakeExchange(authority, payload);
            using var sharedBuffer = ResizableBuffer.Allocate(4096);
            var headersReceived = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var upload = ReadUploadAsync(pipe.ServerReadStream, payload.Length, headersReceived);
            var response = WriteResponseHeadersAsync(pipe.ServerWriteStream, headersReceived.Task);
            var send = pool.Send(
                exchange, null!, sharedBuffer, null!, CancellationToken.None).AsTask();

            await Task.WhenAll(send, response).WaitAsync(TimeSpan.FromSeconds(5));
            var frames = await upload.WaitAsync(TimeSpan.FromSeconds(5));
            await WriteResponseEndStreamAsync(pipe.ServerWriteStream);

            Assert.Equal(
                new[] { expectedPayloadSize, expectedPayloadSize, 123 },
                frames.Select(frame => frame.Payload.Length));
            Assert.Equal(payload, frames.SelectMany(frame => frame.Payload).ToArray());
            Assert.All(frames.Take(frames.Count - 1), frame => Assert.False(frame.EndStream));
            Assert.True(frames[^1].EndStream);
        }

        [Fact]
        public async Task RequestBodyOwnerWaitsUntilFinalWriteConsumesBuffer()
        {
            var payload = CreatePayload(12345);
            var writer = new GatedWriter();
            using var fixture = CreateWorker(payload, writer.Enqueue);
            var bodyBuffer = ResizableBuffer.Allocate(32 * 1024 + 9);

            var owner = ProcessAndDisposeAsync(fixture.Worker, fixture.Exchange, bodyBuffer);

            await writer.Enqueued.Task.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.False(owner.IsCompleted);

            writer.Complete();
            await owner.WaitAsync(TimeSpan.FromSeconds(2));

            var frame = ParseDataFrame(writer.FrameBytes!);
            Assert.Equal(payload, frame.Payload);
            Assert.True(frame.EndStream);
        }

        [Fact]
        public async Task PooledBodyBufferCannotBeOverwrittenBeforeFinalWrite()
        {
            var payload = CreatePayload(8192);
            var writer = new GatedWriter();
            using var fixture = CreateWorker(payload, writer.Enqueue);
            var bodyBuffer = ResizableBuffer.Allocate(32 * 1024 + 9);
            var rentedBytes = bodyBuffer.Buffer;

            var owner = ProcessAndDisposeAsync(fixture.Worker, fixture.Exchange, bodyBuffer);

            await writer.Enqueued.Task.WaitAsync(TimeSpan.FromSeconds(2));

            // Models the corruption possible when an owner returns the body array while
            // the writer still holds its memory and a subsequent pool consumer overwrites it.
            if (owner.IsCompleted)
                rentedBytes.AsSpan().Fill(0xA5);

            writer.Complete();
            await owner.WaitAsync(TimeSpan.FromSeconds(2));

            Assert.Equal(payload, ParseDataFrame(writer.FrameBytes!).Payload);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task FinalWriteFailureOrCancellationSettlesRequestBody(bool cancel)
        {
            var payload = CreatePayload(4096);
            var writer = new GatedWriter();
            using var fixture = CreateWorker(payload, writer.Enqueue);
            using var bodyBuffer = ResizableBuffer.Allocate(32 * 1024 + 9);
            using var cancellation = new CancellationTokenSource();

            var processing = fixture.Worker
                                    .ProcessRequestBody(
                                        fixture.Exchange, bodyBuffer, cancellation.Token)
                                    .AsTask();
            await writer.Enqueued.Task.WaitAsync(TimeSpan.FromSeconds(2));

            if (cancel) {
                cancellation.Cancel();
                writer.Cancel(cancellation.Token);
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => processing);
            }
            else {
                writer.Fail(new IOException("write failed"));
                await Assert.ThrowsAsync<IOException>(() => processing);
            }

            Assert.True(processing.IsCompleted);
        }

        private static async Task NegotiateMaxFrameSize(DuplexPipe pipe, int remoteMaxFrameSize)
        {
            var preface = new byte[H2Constants.Preface.Length];
            await ReadExactlyAsync(pipe.ServerReadStream, preface);
            Assert.Equal(H2Constants.Preface, preface);

            var welcome = await ReadFrameAsync(pipe.ServerReadStream);
            Assert.Equal(H2FrameType.Settings, welcome.Header.BodyType);

            await pipe.ServerWriteStream.WriteAsync(H2FrameHelper.BuildSettingsFrame(
                (SettingIdentifier.SettingsMaxFrameSize, remoteMaxFrameSize)));
            await pipe.ServerWriteStream.FlushAsync();

            (H2Frame Header, byte[] Body) acknowledgement;

            do {
                acknowledgement = await ReadFrameAsync(pipe.ServerReadStream);
            } while (acknowledgement.Header.BodyType != H2FrameType.Settings);

            Assert.True(acknowledgement.Header.Flags.HasFlag(HeaderFlags.Ack));
        }

        private static async Task<List<UploadFrame>> ReadUploadAsync(
            Stream stream, int payloadLength, TaskCompletionSource headersReceived)
        {
            var frames = new List<UploadFrame>();
            var received = 0;

            while (received < payloadLength) {
                var frame = await ReadFrameAsync(stream);

                if (frame.Header.BodyType == H2FrameType.Headers) {
                    headersReceived.TrySetResult();
                    continue;
                }

                if (frame.Header.BodyType != H2FrameType.Data)
                    continue;

                var uploadFrame = new UploadFrame(
                    frame.Body,
                    frame.Header.Flags.HasFlag(HeaderFlags.EndStream));
                frames.Add(uploadFrame);
                received += frame.Body.Length;
            }

            return frames;
        }

        private static async Task<H2Frame> ReadRequestHeadersAsync(
            Stream stream, TaskCompletionSource headersReceived)
        {
            while (true) {
                var frame = await ReadFrameAsync(stream);

                if (frame.Header.BodyType != H2FrameType.Headers)
                    continue;

                headersReceived.TrySetResult();
                return frame.Header;
            }
        }

        private static async Task WriteResponseHeadersAsync(Stream stream, Task headersReceived)
        {
            await headersReceived;
            var response = new byte[10];
            H2Frame.Write(
                response, 1, H2FrameType.Headers, HeaderFlags.EndHeaders, streamIdentifier: 1);
            response[9] = 0x88;
            await stream.WriteAsync(response);
            await stream.FlushAsync();
        }

        private static async Task WriteResponseEndStreamAsync(Stream stream)
        {
            var endStream = new byte[9];
            H2Frame.Write(
                endStream, 0, H2FrameType.Data, HeaderFlags.EndStream, streamIdentifier: 1);
            await stream.WriteAsync(endStream);
            await stream.FlushAsync();
        }

        private static async Task<(H2Frame Header, byte[] Body)> ReadFrameAsync(Stream stream)
        {
            var headerBytes = new byte[9];
            await ReadExactlyAsync(stream, headerBytes);
            var header = new H2Frame(headerBytes);
            var body = new byte[header.BodyLength];
            await ReadExactlyAsync(stream, body);
            return (header, body);
        }

        private static async Task ReadExactlyAsync(Stream stream, byte[] destination)
        {
            var offset = 0;

            while (offset < destination.Length) {
                var read = await stream.ReadAsync(destination.AsMemory(offset));

                if (read == 0)
                    throw new EndOfStreamException();

                offset += read;
            }
        }

        private static async Task ProcessAndDisposeAsync(
            StreamWorker worker, Exchange exchange, ResizableBuffer bodyBuffer)
        {
            using (bodyBuffer)
                await worker.ProcessRequestBody(exchange, bodyBuffer, CancellationToken.None);
        }

        private static WorkerFixture CreateWorker(byte[] payload, UpStreamChannel channel)
        {
            var authority = new Authority("test.local", 443, true);
            var setting = new H2StreamSetting {
                OverallWindowSize = payload.Length + 32 * 1024
            };
            setting.Remote.MaxFrameSize = 32 * 1024;
            setting.Remote.WindowSize = payload.Length + 32 * 1024;
            var memoryProvider = ArrayPoolMemoryProvider<char>.Default;
            var headerEncoder = new HeaderEncoder(
                new HPackEncoder(new EncodingContext(memoryProvider)),
                new HPackDecoder(new DecodingContext(authority, memoryProvider)),
                setting);
            var overallWindow = new WindowSizeHolder(setting.OverallWindowSize, 0);
            var context = new StreamContext(
                connectionId: 1,
                authority: authority,
                setting: setting,
                headerEncoder: headerEncoder,
                upStreamChannel: channel,
                overallWindowSizeHolder: overallWindow);
            var streamPool = new StreamPool(context);
            var resetTokenSource = new CancellationTokenSource();
            var exchange = MakeExchange(authority, payload);
            var worker = new StreamWorker(1, streamPool, exchange, resetTokenSource);
            return new WorkerFixture(
                worker, exchange, streamPool, overallWindow, resetTokenSource);
        }

        private static Exchange MakeExchange(Authority authority, byte[] payload)
        {
            var exchange = new Exchange(
                new TestIdProvider(),
                authority,
                $"POST /upload HTTP/2.0\r\nhost: test.local\r\ncontent-length: {payload.Length}\r\n\r\n".AsMemory(),
                "HTTP/2",
                DateTime.UtcNow);
            exchange.Request.Body = new MemoryStream(payload);
            return exchange;
        }

        private static Exchange MakeBodylessExchange(Authority authority, Stream? body)
        {
            var request = body == null
                ? "GET / HTTP/2.0\r\nhost: test.local\r\n\r\n"
                : "POST /upload HTTP/2.0\r\nhost: test.local\r\ncontent-length: 0\r\n\r\n";
            var exchange = new Exchange(
                new TestIdProvider(), authority, request.AsMemory(), "HTTP/2", DateTime.UtcNow);
            exchange.Request.Body = body;
            return exchange;
        }

        private static byte[] CreatePayload(int length)
        {
            var payload = new byte[length];
            new Random(42).NextBytes(payload);
            return payload;
        }

        private static UploadFrame ParseDataFrame(byte[] frameBytes)
        {
            var header = new H2Frame(frameBytes.AsSpan(0, 9));
            Assert.Equal(H2FrameType.Data, header.BodyType);
            Assert.Equal(frameBytes.Length - 9, header.BodyLength);
            return new UploadFrame(
                frameBytes.AsSpan(9).ToArray(),
                header.Flags.HasFlag(HeaderFlags.EndStream));
        }

        private sealed record UploadFrame(byte[] Payload, bool EndStream);

        private sealed class NonSeekableEmptyStream : Stream
        {
            public int ReadCount { get; private set; }
            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override void Flush() { }

            public override int Read(byte[] buffer, int offset, int count)
            {
                ReadCount++;
                return 0;
            }

            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) =>
                throw new NotSupportedException();
        }

        private sealed class GatedWriter
        {
            private WriteTask _pending;

            public TaskCompletionSource Enqueued { get; } = new(
                TaskCreationOptions.RunContinuationsAsynchronously);

            public byte[]? FrameBytes { get; private set; }

            public void Enqueue(ref WriteTask writeTask)
            {
                _pending = writeTask;
                Enqueued.TrySetResult();
            }

            public void Complete()
            {
                FrameBytes = _pending.BufferBytes.ToArray();
                _pending.OnComplete(null);
            }

            public void Fail(Exception exception) => _pending.OnComplete(exception);

            public void Cancel(CancellationToken token) =>
                _pending.CompletionSource.SetCanceled(token);
        }

        private sealed class WorkerFixture : IDisposable
        {
            private readonly StreamPool _streamPool;
            private readonly WindowSizeHolder _overallWindow;
            private readonly CancellationTokenSource _resetTokenSource;

            public WorkerFixture(
                StreamWorker worker,
                Exchange exchange,
                StreamPool streamPool,
                WindowSizeHolder overallWindow,
                CancellationTokenSource resetTokenSource)
            {
                Worker = worker;
                Exchange = exchange;
                _streamPool = streamPool;
                _overallWindow = overallWindow;
                _resetTokenSource = resetTokenSource;
            }

            public StreamWorker Worker { get; }

            public Exchange Exchange { get; }

            public void Dispose()
            {
                Worker.Dispose();
                _streamPool.Dispose();
                _overallWindow.Dispose();
                _resetTokenSource.Dispose();
            }
        }
    }
}
