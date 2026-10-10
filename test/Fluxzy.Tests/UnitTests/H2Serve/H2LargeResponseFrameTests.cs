// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Fluxzy.Clients.H2;
using Fluxzy.Clients.H2.Frames;
using Fluxzy.Core;
using Fluxzy.Misc.Streams;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.UnitTests.H2Serve
{
    public class H2LargeResponseFrameTests
    {
        [Theory]
        [InlineData(0, 256 * 1024 - 9)]
        [InlineData(0, 256 * 1024 - 8)]
        [InlineData(0, 512 * 1024)]
        [InlineData(1024, 512 * 1024)]
        [InlineData(1024, 1024 * 1024)]
        public async Task QueuedLargeFramesPreservePayloadCompletionAndConnectionReuse(int prefixSize, int largeSize)
        {
            await using var ctx = await H2TestContext.Create();
            await ctx.CompleteHandshake();
            await ctx.SendSettingsFrame(
                (SettingIdentifier.SettingsMaxFrameSize, 1024 * 1024),
                (SettingIdentifier.SettingsInitialWindowSize, 4 * 1024 * 1024));
            await ctx.SendWindowUpdate(0, 4 * 1024 * 1024);
            using var buffer = Fluxzy.Misc.ResizableBuffers.RsBuffer.Allocate(32768);
            await ReceiveRequest(ctx, buffer, 1);
            await ctx.DownStreamPipe.WaitForWriteLoopIdleForTests().WaitAsync(ctx.Token);

            var chunks = new[] { prefixSize, largeSize, 1024 }
                .Where(size => size > 0)
                .Select((size, index) => Enumerable.Repeat((byte) (index + 1), size).ToArray())
                .ToArray();
            var expected = chunks.SelectMany(chunk => chunk).ToArray();
            using var body = new CombinedReadonlyStream(chunks.Select(chunk => new MemoryStream(chunk)), true);

            ctx.DownStreamPipe.PauseWriteLoopForTests();
            try {
                await QueueResponse(ctx, buffer, 1, body, expected.Length);
            }
            finally {
                ctx.DownStreamPipe.ResumeWriteLoopForTests();
            }

            Assert.Equal(expected, await ReadResponse(ctx, 1));
            await ctx.DownStreamPipe.WaitForWriteLoopIdleForTests().WaitAsync(ctx.Token);
            Assert.Equal(0, ctx.DownStreamPipe.ActiveStreamCountForTests);

            await ReceiveRequest(ctx, buffer, 3);
            using var nextBody = new MemoryStream(new byte[] { 42 });
            await QueueResponse(ctx, buffer, 3, nextBody, 1);
            Assert.Equal(new byte[] { 42 }, await ReadResponse(ctx, 3));
        }

        [Fact]
        public async Task OversizedFrameRemainsQueuedWhilePrefixFlushIsInFlight()
        {
            GatedWriteStream? transport = null;
            await using var ctx = await H2TestContext.Create(stream =>
                transport = new GatedWriteStream(stream));
            await ctx.CompleteHandshake();
            await ctx.SendSettingsFrame(
                (SettingIdentifier.SettingsMaxFrameSize, 1024 * 1024),
                (SettingIdentifier.SettingsInitialWindowSize, 4 * 1024 * 1024));
            await ctx.SendWindowUpdate(0, 4 * 1024 * 1024);

            using var buffer = Fluxzy.Misc.ResizableBuffers.RsBuffer.Allocate(32768);
            await ReceiveRequest(ctx, buffer, 1);

            const int prefixSize = 1024;
            const int largeSize = 512 * 1024;
            const int trailingSize = 1024;
            var chunks = new[] {
                Enumerable.Repeat((byte) 1, prefixSize).ToArray(),
                Enumerable.Repeat((byte) 2, largeSize).ToArray(),
                Enumerable.Repeat((byte) 3, trailingSize).ToArray()
            };
            var expected = chunks.SelectMany(chunk => chunk).ToArray();

            await ctx.DownStreamPipe.WriteResponseHeader(
                new ResponseHeader(
                    $"HTTP/1.1 200 OK\r\nContent-Length: {expected.Length}\r\n\r\n".AsMemory(),
                    true, false),
                buffer, false, 1, "GET".AsMemory(), ctx.Token);
            var header = await ctx.ReadUntilFrameType(H2FrameType.Headers);
            Assert.Equal(1, header.StreamIdentifier);
            await ctx.DownStreamPipe.WaitForWriteLoopIdleForTests().WaitAsync(ctx.Token);

            using var body = new CombinedReadonlyStream(
                chunks.Select(chunk => new MemoryStream(chunk)), true);
            ctx.DownStreamPipe.PauseWriteLoopForTests();
            try {
                await ctx.DownStreamPipe.WriteResponseBody(
                    body, buffer, false, 1, null, ctx.Token);
                transport!.GateNextWrite();
                var writeStarted = transport.WriteStarted;
                ctx.DownStreamPipe.ResumeWriteLoopForTests();
                await writeStarted.WaitAsync(ctx.Token);

                // The prefix is owned by the in-flight gather write. The oversized frame must
                // remain at the head of the queue until that write succeeds, so teardown can
                // still return its rented buffer if the write fails or is cancelled.
                Assert.Equal(largeSize + 9, ctx.DownStreamPipe.NextDataFrameLengthForTests);
            }
            finally {
                transport?.ReleaseWrite();
                ctx.DownStreamPipe.ResumeWriteLoopForTests();
            }

            Assert.Equal(expected, await ReadDataFrames(ctx, 1));
        }

        private static async Task ReceiveRequest(H2TestContext ctx, Fluxzy.Misc.ResizableBuffers.RsBuffer buffer, int streamId)
        {
            await ctx.SendHeadersFrame(streamId, "GET /download HTTP/2\r\nHost: localhost\r\n\r\n".AsMemory(),
                endStream: true, endHeaders: true);
            using var scope = new ExchangeScope();
            var exchange = await ctx.DownStreamPipe.ReadNextExchange(buffer, scope, ctx.Token);
            Assert.NotNull(exchange);
            Assert.Equal(streamId, exchange!.StreamIdentifier);
        }

        private static async Task QueueResponse(H2TestContext ctx, Fluxzy.Misc.ResizableBuffers.RsBuffer buffer, int streamId, Stream body, int length)
        {
            await ctx.DownStreamPipe.WriteResponseHeader(
                new ResponseHeader($"HTTP/1.1 200 OK\r\nContent-Length: {length}\r\n\r\n".AsMemory(), true, false),
                buffer, false, streamId, "GET".AsMemory(), ctx.Token);
            await ctx.DownStreamPipe.WriteResponseBody(body, buffer, false, streamId, null, ctx.Token);
        }

        private static async Task<byte[]> ReadResponse(H2TestContext ctx, int streamId)
        {
            var header = await ctx.ReadUntilFrameType(H2FrameType.Headers);
            Assert.Equal(streamId, header.StreamIdentifier);
            Assert.False(header.Flags.HasFlag(HeaderFlags.EndStream));
            return await ReadDataFrames(ctx, streamId);
        }

        private static async Task<byte[]> ReadDataFrames(H2TestContext ctx, int streamId)
        {
            using var body = new MemoryStream();
            while (true) {
                var frame = await ctx.ReadNextFrame(H2FrameType.Data);
                Assert.Equal(streamId, frame.StreamIdentifier);
                body.Write(frame.GetDataFrame().Buffer.Span);
                if (frame.Flags.HasFlag(HeaderFlags.EndStream))
                    return body.ToArray();
            }
        }
    }
}
