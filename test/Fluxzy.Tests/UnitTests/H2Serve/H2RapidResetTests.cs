// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fluxzy.Clients.H2;
using Fluxzy.Clients.H2.Frames;
using Fluxzy.Core;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.UnitTests.H2Serve
{
    public class H2RapidResetTests
    {
        [Fact]
        public async Task ServerSettingsAdvertiseMaxConcurrentStreams()
        {
            await using var ctx = await H2TestContext.Create();
            var settings = await ctx.ReadNextFrame(H2FrameType.Settings);

            Assert.Equal(256, ReadSettings(settings)[SettingIdentifier.SettingsMaxConcurrentStreams]);
        }

        private static Dictionary<SettingIdentifier, int> ReadSettings(H2FrameReadResult frame)
        {
            var advertised = new Dictionary<SettingIdentifier, int>();
            var index = 0;

            while (frame.TryReadNextSetting(out var setting, ref index))
                advertised[setting.SettingIdentifier] = setting.Value;

            return advertised;
        }

        [Fact]
        public async Task StreamsBeyondMaxConcurrentAreRefused()
        {
            var setting = new H2StreamSetting { MaxStreamResetsPerWindow = 100 };
            setting.Local.SettingsMaxConcurrentStreams = 2;
            await using var ctx = await H2TestContext.Create(h2StreamSetting: setting);
            await CompleteHandshake(ctx);
            using var buffer = Fluxzy.Misc.ResizableBuffers.RsBuffer.Allocate(32768);
            using var scope = new ExchangeScope();

            for (var streamId = 1; streamId <= 3; streamId += 2) {
                await ctx.SendHeadersFrame(streamId,
                    $"GET /{streamId} HTTP/2\r\nHost: localhost\r\n\r\n".AsMemory(),
                    endStream: true, endHeaders: true);
                Assert.NotNull(await ctx.DownStreamPipe.ReadNextExchange(buffer, scope, ctx.Token));
            }

            await ctx.SendHeadersFrame(5,
                "GET /5 HTTP/2\r\nHost: localhost\r\nx-rapid: dynamic-table-entry\r\n\r\n".AsMemory(),
                endStream: true, endHeaders: true);
            var reset = await ReadUntilFrame(ctx, H2FrameType.RstStream);
            Assert.Equal(5, reset.StreamIdentifier);
            Assert.Equal(H2ErrorCode.RefusedStream, reset.GetRstStreamFrame().ErrorCode);

            var pending = ctx.DownStreamPipe.ReadNextExchange(buffer, scope, ctx.Token).AsTask();
            await PingBarrier(ctx, 1);
            Assert.False(pending.IsCompleted);
            Assert.Equal(2, ctx.DownStreamPipe.ActiveStreamCountForTests);

            await ctx.DownStreamPipe.WriteResponseHeader(
                new ResponseHeader("HTTP/1.1 204 No Content\r\nContent-Length: 0\r\n\r\n".AsMemory(), true, false),
                buffer, false, 1, "GET".AsMemory(), ctx.Token);
            var response = await ReadUntilFrame(ctx, H2FrameType.Headers);
            Assert.Equal(1, response.StreamIdentifier);
            await PingBarrier(ctx, 2);
            Assert.Equal(1, ctx.DownStreamPipe.ActiveStreamCountForTests);

            await ctx.SendHeadersFrame(7,
                "GET /7 HTTP/2\r\nHost: localhost\r\nx-rapid: dynamic-table-entry\r\n\r\n".AsMemory(),
                endStream: true, endHeaders: true);
            var exchange = await pending.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.NotNull(exchange);
            Assert.Equal(7, exchange!.StreamIdentifier);
            Assert.Equal("/7", exchange.Request.Header.Path.ToString());
            Assert.Contains(exchange.Request.Header.Headers,
                h => h.Name.Span.SequenceEqual("x-rapid") && h.Value.Span.SequenceEqual("dynamic-table-entry"));
        }

        [Fact]
        public async Task RapidResetFloodClosesConnectionWithEnhanceYourCalm()
        {
            var setting = new H2StreamSetting { MaxStreamResetsPerWindow = 3 };
            await using var ctx = await H2TestContext.Create(h2StreamSetting: setting);
            await CompleteHandshake(ctx);

            for (var streamId = 1; streamId <= 5; streamId += 2) {
                await SendHeadersThenReset(ctx, streamId);
            }

            await PingBarrier(ctx, 1);
            await SendHeadersThenReset(ctx, 7);

            var goAway = await ReadUntilFrame(ctx, H2FrameType.Goaway);
            goAway.GetGoAwayFrame().Read(out var errorCode, out var lastStreamId);
            Assert.Equal(H2ErrorCode.EnhanceYourCalm, errorCode);
            Assert.Equal(7, lastStreamId);

            using var buffer = Fluxzy.Misc.ResizableBuffers.RsBuffer.Allocate(32768);
            using var scope = new ExchangeScope();
            var drained = 0;

            while (await ctx.DownStreamPipe.ReadNextExchange(buffer, scope, ctx.Token) != null)
                drained++;

            Assert.Equal(4, drained);
            Assert.Equal(0, ctx.DownStreamPipe.ActiveStreamCountForTests);
        }

        [Fact]
        public async Task ResetBudgetRenewsAfterWindowElapses()
        {
            var setting = new H2StreamSetting {
                MaxStreamResetsPerWindow = 2,
                StreamResetWindow = TimeSpan.FromMilliseconds(100)
            };
            await using var ctx = await H2TestContext.Create(h2StreamSetting: setting);
            await CompleteHandshake(ctx);

            await SendHeadersThenReset(ctx, 1);
            await SendHeadersThenReset(ctx, 3);
            await Task.Delay(400);
            await SendHeadersThenReset(ctx, 5);
            await SendHeadersThenReset(ctx, 7);
            await PingBarrier(ctx, 1);

            await SendHeadersThenReset(ctx, 9);
            var goAway = await ReadUntilFrame(ctx, H2FrameType.Goaway);
            goAway.GetGoAwayFrame().Read(out var errorCode, out _);
            Assert.Equal(H2ErrorCode.EnhanceYourCalm, errorCode);
        }

        private static async Task SendHeadersThenReset(H2TestContext ctx, int streamId)
        {
            await ctx.SendHeadersFrame(streamId,
                $"GET /{streamId} HTTP/2\r\nHost: localhost\r\n\r\n".AsMemory(),
                endStream: true, endHeaders: true);
            await ctx.SendRstStream(streamId, H2ErrorCode.Cancel);
        }

        private static async Task CompleteHandshake(H2TestContext ctx)
        {
            await ctx.CompleteHandshake();
            var settingsAck = await ReadUntilFrame(ctx, H2FrameType.Settings);
            Assert.True(settingsAck.Flags.HasFlag(HeaderFlags.Ack));
        }

        private static async Task<H2FrameReadResult> ReadUntilFrame(H2TestContext ctx, H2FrameType frameType)
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
    }
}
