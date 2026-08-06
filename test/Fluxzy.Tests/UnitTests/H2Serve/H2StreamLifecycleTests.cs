// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Fluxzy.Clients.H2;
using Fluxzy.Clients.H2.Encoder;
using Fluxzy.Clients.H2.Frames;
using Fluxzy.Core;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.UnitTests.H2Serve
{
    public class H2StreamLifecycleTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task RequestAndResponseHalvesCloseInEitherOrder(bool responseFirst)
        {
            await using var ctx = await H2TestContext.Create();
            await CompleteHandshake(ctx);
            await ctx.SendHeadersFrame(1,
                "POST /order HTTP/2\r\nHost: localhost\r\nContent-Length: 3\r\n\r\n".AsMemory(),
                endStream: false, endHeaders: true);

            using var buffer = Fluxzy.Misc.ResizableBuffers.RsBuffer.Allocate(32768);
            using var scope = new ExchangeScope();
            var exchange = await ctx.DownStreamPipe.ReadNextExchange(buffer, scope, ctx.Token);
            Assert.NotNull(exchange);
            var body = ReadBody(exchange!.Request.Body!, ctx);

            if (responseFirst) {
                await WriteBodylessResponse(ctx, buffer, 1, "POST");
                await ReadStreamFrames(ctx, 1, 1);
                await PingBarrier(ctx, 11);
                Assert.Equal(1, ctx.DownStreamPipe.ActiveStreamCountForTests);
                await ctx.SendDataFrame(1, Encoding.ASCII.GetBytes("req"), endStream: true);
            }
            else {
                await ctx.SendDataFrame(1, Encoding.ASCII.GetBytes("req"), endStream: true);
                Assert.Equal("req", await body);
                await PingBarrier(ctx, 12);
                Assert.Equal(1, ctx.DownStreamPipe.ActiveStreamCountForTests);
                await WriteBodylessResponse(ctx, buffer, 1, "POST");
                await ReadStreamFrames(ctx, 1, 1);
            }

            Assert.Equal("req", await body);
            await PingBarrier(ctx, 13);
            Assert.Equal(0, ctx.DownStreamPipe.ActiveStreamCountForTests);
            Assert.Equal(1, ctx.DownStreamPipe.ClosedStreamCountForTests);
        }

        [Theory]
        [InlineData(ResponseTerminal.BodylessHeaders)]
        [InlineData(ResponseTerminal.FinalData)]
        [InlineData(ResponseTerminal.Trailers)]
        public async Task TerminalResponsePathsCloseAfterTheirWrites(ResponseTerminal terminal)
        {
            await using var ctx = await H2TestContext.Create();
            await CompleteHandshake(ctx);
            using var buffer = Fluxzy.Misc.ResizableBuffers.RsBuffer.Allocate(32768);

            for (var iteration = 0; iteration < 5; iteration++) {
                var streamIdentifier = iteration * 2 + 1;
                await ctx.SendHeadersFrame(streamIdentifier,
                    $"GET /terminal/{iteration} HTTP/2\r\nHost: localhost\r\n\r\n".AsMemory(),
                    endStream: true, endHeaders: true);
                using var scope = new ExchangeScope();
                var exchange = await ctx.DownStreamPipe.ReadNextExchange(
                    buffer, scope, ctx.Token);
                Assert.NotNull(exchange);

                await WriteResponse(ctx, buffer, streamIdentifier, terminal);
                var frames = await ReadStreamFrames(
                    ctx, streamIdentifier, terminal == ResponseTerminal.BodylessHeaders ? 1 : 3);

                Assert.True(frames[^1].Flags.HasFlag(HeaderFlags.EndStream));
                Assert.Equal(terminal == ResponseTerminal.Trailers
                        ? H2FrameType.Headers
                        : terminal == ResponseTerminal.FinalData
                            ? H2FrameType.Data
                            : H2FrameType.Headers,
                    frames[^1].BodyType);
                await PingBarrier(ctx, 100 + iteration);
                Assert.Equal(0, ctx.DownStreamPipe.ActiveStreamCountForTests);
                Assert.Equal(iteration + 1, ctx.DownStreamPipe.ClosedStreamCountForTests);
            }
        }

        [Fact]
        public async Task TerminalWriteGateRetainsWorkerUntilWriteSucceeds()
        {
            GatedWriteStream? transport = null;
            await using var ctx = await H2TestContext.Create(stream =>
                transport = new GatedWriteStream(stream));
            await CompleteHandshake(ctx);
            await ctx.SendHeadersFrame(1,
                "GET /gated HTTP/2\r\nHost: localhost\r\n\r\n".AsMemory(),
                endStream: true, endHeaders: true);

            using var buffer = Fluxzy.Misc.ResizableBuffers.RsBuffer.Allocate(32768);
            using var scope = new ExchangeScope();
            Assert.NotNull(await ctx.DownStreamPipe.ReadNextExchange(buffer, scope, ctx.Token));
            transport!.GateNextWrite();

            await WriteBodylessResponse(ctx, buffer, 1, "GET");
            await transport.WriteStarted.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.Equal(1, ctx.DownStreamPipe.ActiveStreamCountForTests);
            Assert.Equal(0, ctx.DownStreamPipe.ClosedStreamCountForTests);

            transport.ReleaseWrite();
            await ReadStreamFrames(ctx, 1, 1);
            await PingBarrier(ctx, 201);
            Assert.Equal(0, ctx.DownStreamPipe.ActiveStreamCountForTests);
            Assert.Equal(1, ctx.DownStreamPipe.ClosedStreamCountForTests);
        }

        [Fact]
        public async Task FragmentedInitialEndStreamCompletesOnlyAfterContinuation()
        {
            await using var ctx = await H2TestContext.Create();
            await CompleteHandshake(ctx);
            var fragments = ctx.CreateFragmentedHeadersFrames(1,
                "GET /fragmented HTTP/2\r\nHost: localhost\r\n\r\n".AsMemory(),
                endStream: true);

            var streamActive = ctx.DownStreamPipe.WaitForStreamActiveForTests(1);
            await ctx.SendRawFrame(fragments.Headers);
            await streamActive.WaitAsync(TimeSpan.FromSeconds(2));
            using var buffer = Fluxzy.Misc.ResizableBuffers.RsBuffer.Allocate(32768);
            using var scope = new ExchangeScope();
            var exchangeTask = ctx.DownStreamPipe.ReadNextExchange(
                buffer, scope, ctx.Token).AsTask();
            Assert.False(exchangeTask.IsCompleted);
            Assert.Equal(1, ctx.DownStreamPipe.ActiveStreamCountForTests);

            await ctx.SendRawFrame(fragments.Continuation);
            var exchange = await exchangeTask.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.NotNull(exchange);
            Assert.Same(Stream.Null, exchange!.Request.Body);

            await WriteBodylessResponse(ctx, buffer, 1, "GET");
            await ReadStreamFrames(ctx, 1, 1);
            await PingBarrier(ctx, 301);
            Assert.Equal(0, ctx.DownStreamPipe.ActiveStreamCountForTests);
        }

        [Fact]
        public async Task ResetFaultsRequestCancelsResponseAndDropsQueuedBuffers()
        {
            await using var ctx = await H2TestContext.Create();
            await CompleteHandshake(ctx);
            await ctx.SendHeadersFrame(1,
                "POST /reset HTTP/2\r\nHost: localhost\r\nContent-Length: 1\r\n\r\n".AsMemory(),
                endStream: false, endHeaders: true);

            using var buffer = Fluxzy.Misc.ResizableBuffers.RsBuffer.Allocate(32768);
            using var scope = new ExchangeScope();
            var exchange = await ctx.DownStreamPipe.ReadNextExchange(buffer, scope, ctx.Token);
            Assert.NotNull(exchange);
            var requestBody = ReadBody(exchange!.Request.Body!, ctx);
            ctx.DownStreamPipe.PauseWriteLoopForTests();

            try {
                await ctx.DownStreamPipe.WriteResponseHeader(
                    new ResponseHeader(
                        "HTTP/1.1 200 OK\r\nContent-Length: 70000\r\n\r\n".AsMemory(),
                        true, false),
                    buffer, false, 1, "POST".AsMemory(), ctx.Token);
                var queued = ctx.DownStreamPipe.WaitForResponseDataEntriesForTests(3);
                var response = ctx.DownStreamPipe.WriteResponseBody(
                    new MemoryStream(new byte[70000]), buffer, false, 1,
                    new Response(), ctx.Token).AsTask();
                await queued.WaitAsync(TimeSpan.FromSeconds(2));
                Assert.False(response.IsCompleted);

                await ctx.SendRstStream(1, H2ErrorCode.Cancel);
                await Assert.ThrowsAnyAsync<Exception>(async () => await requestBody);
                await response.WaitAsync(TimeSpan.FromSeconds(2));
                Assert.Equal(0, ctx.DownStreamPipe.ActiveStreamCountForTests);
                Assert.Equal(1, ctx.DownStreamPipe.ClosedStreamCountForTests);

                var idle = ctx.DownStreamPipe.WaitForWriteLoopIdleForTests();
                ctx.DownStreamPipe.ResumeWriteLoopForTests();
                await idle.WaitAsync(TimeSpan.FromSeconds(2));
                Assert.Equal(3, ctx.DownStreamPipe.DroppedResponseBufferCountForTests);

                await ctx.SendDataFrame(1, Encoding.ASCII.GetBytes("late"), endStream: true);
                await PingBarrier(ctx, 401);
                Assert.Equal(0, ctx.DownStreamPipe.ActiveStreamCountForTests);
            }
            finally {
                ctx.DownStreamPipe.ResumeWriteLoopForTests();
            }
        }

        [Fact]
        public async Task StreamProtocolErrorAbortsOwnedRequestBody()
        {
            await using var ctx = await H2TestContext.Create();
            await CompleteHandshake(ctx);
            await ctx.SendHeadersFrame(1,
                "POST /protocol HTTP/2\r\nHost: localhost\r\n\r\n".AsMemory(),
                endStream: false, endHeaders: true);

            using var buffer = Fluxzy.Misc.ResizableBuffers.RsBuffer.Allocate(32768);
            using var scope = new ExchangeScope();
            var exchange = await ctx.DownStreamPipe.ReadNextExchange(buffer, scope, ctx.Token);
            Assert.NotNull(exchange);
            var requestBody = ReadBody(exchange!.Request.Body!, ctx);

            await ctx.SendHeadersFrame(1,
                "x-invalid: trailer without end stream\r\n\r\n".AsMemory(),
                endStream: false, endHeaders: true);
            await Assert.ThrowsAnyAsync<Exception>(async () => await requestBody);
            var reset = await ReadUntilFrame(ctx, H2FrameType.RstStream);
            Assert.Equal(1, reset.StreamIdentifier);
            Assert.Equal(0, ctx.DownStreamPipe.ActiveStreamCountForTests);
            Assert.Equal(1, ctx.DownStreamPipe.ClosedStreamCountForTests);
        }

        private static async Task CompleteHandshake(H2TestContext ctx)
        {
            await ctx.CompleteHandshake();
            var settingsAck = await ReadUntilFrame(ctx, H2FrameType.Settings);
            Assert.True(settingsAck.Flags.HasFlag(HeaderFlags.Ack));
        }

        private static async Task WriteResponse(
            H2TestContext ctx, Fluxzy.Misc.ResizableBuffers.RsBuffer buffer, int streamIdentifier,
            ResponseTerminal terminal)
        {
            if (terminal == ResponseTerminal.BodylessHeaders) {
                await WriteBodylessResponse(ctx, buffer, streamIdentifier, "GET");
                return;
            }

            await ctx.DownStreamPipe.WriteResponseHeader(
                new ResponseHeader(
                    "HTTP/1.1 200 OK\r\nContent-Length: 1\r\n\r\n".AsMemory(),
                    true, false),
                buffer, false, streamIdentifier, "GET".AsMemory(), ctx.Token);
            var response = new Response();

            if (terminal == ResponseTerminal.Trailers) {
                response.Trailers = new List<HeaderField> { new("x-terminal", "trailer") };
            }

            await ctx.DownStreamPipe.WriteResponseBody(
                new MemoryStream(new byte[] { 1 }), buffer, false,
                streamIdentifier, response, ctx.Token);
        }

        private static ValueTask WriteBodylessResponse(
            H2TestContext ctx, Fluxzy.Misc.ResizableBuffers.RsBuffer buffer,
            int streamIdentifier, string method)
            => ctx.DownStreamPipe.WriteResponseHeader(
                new ResponseHeader(
                    "HTTP/1.1 204 No Content\r\nContent-Length: 0\r\n\r\n".AsMemory(),
                    true, false),
                buffer, false, streamIdentifier, method.AsMemory(), ctx.Token);

        private static async Task<List<H2FrameReadResult>> ReadStreamFrames(
            H2TestContext ctx, int streamIdentifier, int count)
        {
            var frames = new List<H2FrameReadResult>();

            while (frames.Count < count) {
                var frame = await ctx.ReadNextFrame();
                Assert.NotEqual(H2FrameType.RstStream, frame.BodyType);

                if (frame.StreamIdentifier == streamIdentifier)
                    frames.Add(frame);
            }

            return frames;
        }

        private static async Task<H2FrameReadResult> ReadUntilFrame(
            H2TestContext ctx, H2FrameType frameType)
        {
            while (true) {
                var frame = await ctx.ReadNextFrame();

                if (frame.BodyType == frameType)
                    return frame;
            }
        }

        private static async Task PingBarrier(H2TestContext ctx, long opaqueData)
        {
            await ctx.SendPing(opaqueData);
            var ping = await ReadUntilFrame(ctx, H2FrameType.Ping);
            Assert.True(ping.Flags.HasFlag(HeaderFlags.Ack));
        }

        private static async Task<string> ReadBody(Stream body, H2TestContext ctx)
        {
            using var destination = new MemoryStream();
            await body.CopyToAsync(destination, ctx.Token);
            return Encoding.ASCII.GetString(destination.ToArray());
        }

        public enum ResponseTerminal
        {
            BodylessHeaders,
            FinalData,
            Trailers
        }
    }
}
