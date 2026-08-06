// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Clients.H2;
using Fluxzy.Clients.H2.Encoder;
using Fluxzy.Clients.H2.Encoder.Utils;
using Fluxzy.Clients.H2.Frames;
using Fluxzy.Core;
using Xunit;

namespace Fluxzy.Tests.UnitTests.H2Client
{
    public class StreamWorkerResponsePipeTests
    {
        [Fact]
        public async Task HeadersEndingStream_PublishStreamNullWithoutCreatingPipe()
        {
            using var fixture = new WorkerFixture();

            fixture.ReceiveResponseHeaders(endStream: true);

            await fixture.Worker.ProcessResponse(CancellationToken.None, null!);

            Assert.Same(Stream.Null, fixture.Exchange.Response.Body);
            Assert.Null(fixture.Worker.ResponseBodyPipeForTests);
        }

        [Fact]
        public async Task DataBeforeProcessResponse_CreatesOnePipeAndPreservesBody()
        {
            using var fixture = new WorkerFixture();
            var body = new byte[] { 1, 2, 3, 4 };

            fixture.ReceiveResponseHeaders(endStream: false);
            fixture.Worker.ReceiveBodyFragmentFromConnection(body, endStream: true);
            var pipe = fixture.Worker.ResponseBodyPipeForTests;

            await fixture.Worker.ProcessResponse(CancellationToken.None, null!);

            Assert.NotNull(pipe);
            Assert.Same(pipe, fixture.Worker.ResponseBodyPipeForTests);
            Assert.Equal(body, await ReadAllBytes(fixture.Exchange.Response.Body!));
        }

        [Fact]
        public async Task DataAfterProcessResponse_ReusesPublishedPipe()
        {
            using var fixture = new WorkerFixture();
            var body = new byte[] { 5, 6, 7, 8 };

            fixture.ReceiveResponseHeaders(endStream: false);
            await fixture.Worker.ProcessResponse(CancellationToken.None, null!);
            var pipe = fixture.Worker.ResponseBodyPipeForTests;

            fixture.Worker.ReceiveBodyFragmentFromConnection(body, endStream: true);

            Assert.NotNull(pipe);
            Assert.Same(pipe, fixture.Worker.ResponseBodyPipeForTests);
            Assert.Equal(body, await ReadAllBytes(fixture.Exchange.Response.Body!));
        }

        [Fact]
        public async Task ConcurrentDataAndProcessResponse_CreateOnePipe()
        {
            using var fixture = new WorkerFixture();
            using var start = new ManualResetEventSlim();

            fixture.ReceiveResponseHeaders(endStream: false);

            var processResponse = Task.Run(async () => {
                start.Wait();
                await fixture.Worker.ProcessResponse(CancellationToken.None, null!);
            });
            var receiveData = Task.Run(() => {
                start.Wait();
                fixture.Worker.ReceiveBodyFragmentFromConnection(new byte[] { 9 }, endStream: true);
            });

            start.Set();
            await Task.WhenAll(processResponse, receiveData);

            var pipe = fixture.Worker.ResponseBodyPipeForTests;
            Assert.NotNull(pipe);
            Assert.Equal(new byte[] { 9 }, await ReadAllBytes(fixture.Exchange.Response.Body!));
            Assert.Same(pipe, fixture.Worker.ResponseBodyPipeForTests);
        }

        [Fact]
        public async Task ConcurrentResetAndDispose_CompleteBodyOnceWithoutThrowing()
        {
            using var fixture = new WorkerFixture();

            fixture.ReceiveResponseHeaders(endStream: false);
            await fixture.Worker.ProcessResponse(CancellationToken.None, null!);

            await Task.WhenAll(
                Task.Run(() => fixture.Worker.ResetRequest(H2ErrorCode.Cancel)),
                Task.Run(fixture.Worker.Dispose),
                Task.Run(fixture.Worker.Dispose));

            Assert.Equal(0, await fixture.Exchange.Response.Body!.ReadAsync(new byte[1]));

            fixture.Worker.ResetRequest(H2ErrorCode.Cancel);
            await Assert.ThrowsAsync<ExchangeException>(() => fixture.Exchange.Complete);
        }

        [Fact]
        public async Task ResetAfterDataRelaysReceivedBodyWithCleanEndOfStream()
        {
            using var fixture = new WorkerFixture();
            var body = new byte[] { 1, 2, 3 };

            fixture.ReceiveResponseHeaders(endStream: false);
            await fixture.Worker.ProcessResponse(CancellationToken.None, null!);
            fixture.Worker.ReceiveBodyFragmentFromConnection(body, endStream: false);

            // Some servers reset instead of sending END_STREAM after a full
            // response. The received body must be relayed as a clean EOF while
            // the exchange itself records the reset.
            fixture.Worker.ResetRequest(H2ErrorCode.InternalError);

            Assert.Equal(body, await ReadAllBytes(fixture.Exchange.Response.Body!));
            await Assert.ThrowsAsync<ExchangeException>(() => fixture.Exchange.Complete);
        }

        [Fact]
        public async Task ResetBeforeResponsePublication_DoesNotCreatePipe()
        {
            using var fixture = new WorkerFixture();

            fixture.Worker.ResetRequest(H2ErrorCode.Cancel);

            Assert.Null(fixture.Worker.ResponseBodyPipeForTests);
            await Assert.ThrowsAsync<ExchangeException>(() => fixture.Exchange.Complete);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task TrailersCompleteBodyBeforeOrAfterResponsePublication(bool publishFirst)
        {
            using var fixture = new WorkerFixture();

            fixture.ReceiveResponseHeaders(endStream: false);

            if (publishFirst)
                await fixture.Worker.ProcessResponse(CancellationToken.None, null!);

            fixture.ReceiveTrailers(new HeaderField("x-result", "complete"));

            if (!publishFirst)
                await fixture.Worker.ProcessResponse(CancellationToken.None, null!);

            Assert.Equal("complete", Assert.Single(fixture.Exchange.Response.Trailers!).Value.ToString());
            Assert.Empty(await ReadAllBytes(fixture.Exchange.Response.Body!));

            if (publishFirst)
                Assert.NotNull(fixture.Worker.ResponseBodyPipeForTests);
            else
                Assert.Null(fixture.Worker.ResponseBodyPipeForTests);
        }

        [Fact]
        public async Task EmptyDataBeforeProcessResponse_CreatesCompletedPipe()
        {
            using var fixture = new WorkerFixture();

            fixture.ReceiveResponseHeaders(endStream: false);
            fixture.Worker.ReceiveBodyFragmentFromConnection(ReadOnlyMemory<byte>.Empty, endStream: true);
            var pipe = fixture.Worker.ResponseBodyPipeForTests;

            await fixture.Worker.ProcessResponse(CancellationToken.None, null!);

            Assert.NotNull(pipe);
            Assert.Same(pipe, fixture.Worker.ResponseBodyPipeForTests);
            Assert.NotSame(Stream.Null, fixture.Exchange.Response.Body);
            Assert.Empty(await ReadAllBytes(fixture.Exchange.Response.Body!));
        }

        private static async Task<byte[]> ReadAllBytes(Stream stream)
        {
            await using var destination = new MemoryStream();
            await stream.CopyToAsync(destination);
            return destination.ToArray();
        }

        private sealed class WorkerFixture : IDisposable
        {
            private readonly HPackEncoder _responseEncoder;
            private readonly StreamPool _streamPool;
            private readonly WindowSizeHolder _overallWindow;
            private readonly CancellationTokenSource _resetTokenSource;

            public WorkerFixture()
            {
                var authority = new Authority("test.local", 443, true);
                var setting = new H2StreamSetting();
                var memoryProvider = ArrayPoolMemoryProvider<char>.Default;
                var requestEncoder = new HPackEncoder(new EncodingContext(memoryProvider));
                var responseDecoder = new HPackDecoder(new DecodingContext(authority, memoryProvider));
                var headerEncoder = new HeaderEncoder(requestEncoder, responseDecoder, setting);

                _responseEncoder = new HPackEncoder(new EncodingContext(memoryProvider));
                _overallWindow = new WindowSizeHolder(setting.OverallWindowSize, 0);
                _resetTokenSource = new CancellationTokenSource();

                var context = new StreamContext(
                    connectionId: 1,
                    authority: authority,
                    setting: setting,
                    headerEncoder: headerEncoder,
                    upStreamChannel: static (ref WriteTask _) => { },
                    overallWindowSizeHolder: _overallWindow);

                _streamPool = new StreamPool(context);
                Exchange = new Exchange(
                    IIdProvider.FromZero,
                    authority,
                    "GET / HTTP/2.0\r\nhost: test.local\r\n\r\n".AsMemory(),
                    "HTTP/2",
                    DateTime.UtcNow);
                Worker = new StreamWorker(1, _streamPool, Exchange, _resetTokenSource);
            }

            public Exchange Exchange { get; }

            public StreamWorker Worker { get; }

            public void ReceiveResponseHeaders(bool endStream)
            {
                var fields = new List<HeaderField> { new(":status", "200") };
                ReceiveHeaders(fields, endStream);
            }

            public void ReceiveTrailers(params HeaderField[] trailers)
            {
                ReceiveHeaders(trailers, endStream: true);
            }

            public void Dispose()
            {
                Worker.Dispose();
                _streamPool.Dispose();
                _overallWindow.Dispose();
                _resetTokenSource.Dispose();
                _responseEncoder.Dispose();
            }

            private void ReceiveHeaders(IList<HeaderField> fields, bool endStream)
            {
                var buffer = new byte[1024];
                var encodedLength = _responseEncoder.EncodeFields(fields, buffer).Length;
                var flags = HeaderFlags.EndHeaders |
                            (endStream ? HeaderFlags.EndStream : HeaderFlags.None);
                var frame = new HeadersFrame(buffer.AsMemory(0, encodedLength), flags);

                Worker.ReceiveHeaderFragmentFromConnection(ref frame);
            }
        }
    }
}
