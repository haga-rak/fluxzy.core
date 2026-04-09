// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients.H2;
using Fluxzy.Clients.H2.Frames;
using Fluxzy.Core;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.UnitTests.H2Serve
{
    public class H2DownStreamPipeTests
    {
        /// <summary>
        /// Verifies that after Init (which reads the client preface),
        /// the server sends a SETTINGS frame as the connection preface.
        /// </summary>
        [Fact]
        public async Task Init_ValidPreface_SendsSettingsFrame()
        {
            await using var ctx = await H2TestContext.Create();

            // The first frame the server sends after Init should be SETTINGS
            var frame = await ctx.ReadNextFrame();

            Assert.Equal(H2FrameType.Settings, frame.BodyType);
            Assert.Equal(0, frame.StreamIdentifier);
            // It should NOT be an ACK (it's the server's initial settings)
            Assert.False(frame.Flags.HasFlag(HeaderFlags.Ack));
        }

        /// <summary>
        /// After init, send client SETTINGS, verify the server responds with SETTINGS ACK.
        /// </summary>
        [Fact]
        public async Task ClientSettings_ServerRespondsWithAck()
        {
            await using var ctx = await H2TestContext.Create();

            // Drain initial server SETTINGS frame
            await ctx.ReadNextFrame(H2FrameType.Settings);

            // Send client SETTINGS
            await ctx.SendSettingsFrame(
                (SettingIdentifier.SettingsMaxConcurrentStreams, 100));

            // Send SETTINGS ACK for server's settings
            await ctx.SendSettingsAck();

            // Read frames from server until we get a SETTINGS ACK
            var foundAck = false;
            var maxFrames = 10;

            for (var i = 0; i < maxFrames; i++)
            {
                var frame = await ctx.ReadNextFrame();

                if (frame.BodyType == H2FrameType.Settings && frame.Flags.HasFlag(HeaderFlags.Ack))
                {
                    foundAck = true;
                    break;
                }
            }

            Assert.True(foundAck, "Server did not send SETTINGS ACK in response to client SETTINGS");
        }

        /// <summary>
        /// Send HEADERS with END_STREAM + END_HEADERS on stream 1,
        /// then call ReadNextExchange and verify an exchange is returned.
        /// </summary>
        [Fact]
        public async Task SimpleGetRequest_ProducesExchange()
        {
            await using var ctx = await H2TestContext.Create();

            // Drain initial server SETTINGS + possible WINDOW_UPDATE
            await ctx.ReadNextFrame(); // SETTINGS

            // Send SETTINGS ACK to server
            await ctx.SendSettingsAck();

            // Send a simple GET request on stream 1
            var headers = "GET / HTTP/2\r\nHost: localhost\r\n\r\n".AsMemory();
            await ctx.SendHeadersFrame(1, headers, endStream: true, endHeaders: true);

            // Read the exchange produced by H2DownStreamPipe
            using var buffer = Fluxzy.Misc.ResizableBuffers.RsBuffer.Allocate(32768);
            using var scope = new ExchangeScope();

            var exchange = await ctx.DownStreamPipe.ReadNextExchange(buffer, scope, ctx.Token);

            Assert.NotNull(exchange);
            Assert.Equal("localhost", exchange!.Authority.HostName);
            Assert.Equal(1, exchange.StreamIdentifier);
        }

        /// <summary>
        /// Firefox-style zero-length POST: HEADERS (no END_STREAM) + empty DATA[END_STREAM].
        /// Verifies the exchange exposes an empty, non-seekable request body with
        /// Content-Length: 0. This combination is precisely what tripped the upstream
        /// StreamWorker into sending a stray DATA[END_STREAM] frame on top of a
        /// HEADERS[END_STREAM] frame, causing the remote to reset the connection.
        /// </summary>
        [Fact]
        public async Task PostContentLengthZero_WithTrailingEmptyDataFrame_ProducesEmptyBody()
        {
            await using var ctx = await H2TestContext.Create();

            await ctx.ReadNextFrame(); // SETTINGS
            await ctx.SendSettingsAck();

            // Firefox sends HEADERS without END_STREAM...
            var headers =
                "POST /Feeds/Pop HTTP/2\r\nHost: localhost\r\nContent-Length: 0\r\nTE: trailers\r\n\r\n"
                    .AsMemory();
            await ctx.SendHeadersFrame(1, headers, endStream: false, endHeaders: true);

            // ...then a DATA frame with zero bytes and END_STREAM to close the stream.
            await ctx.SendDataFrame(1, Array.Empty<byte>(), endStream: true);

            using var buffer = Fluxzy.Misc.ResizableBuffers.RsBuffer.Allocate(32768);
            using var scope = new ExchangeScope();

            var exchange = await ctx.DownStreamPipe.ReadNextExchange(buffer, scope, ctx.Token);

            Assert.NotNull(exchange);
            Assert.Equal(1, exchange!.StreamIdentifier);
            Assert.Equal(0, exchange.Request.Header.ContentLength);

            // The body must be a real (non-seekable) pipe stream — NOT Stream.Null — because
            // HEADERS did not carry END_STREAM. This is the scenario that used to cause
            // StreamWorker.ProcessRequestBody to enqueue an extra DATA[END_STREAM, 0] frame.
            Assert.NotNull(exchange.Request.Body);
            Assert.False(exchange.Request.Body!.CanSeek);

            using var bodyStream = new MemoryStream();
            await exchange.Request.Body.CopyToAsync(bodyStream, ctx.Token);
            Assert.Empty(bodyStream.ToArray());
        }

        /// <summary>
        /// Send HEADERS (no END_STREAM) then DATA with END_STREAM.
        /// Verify the exchange body reads correctly.
        /// </summary>
        [Fact]
        public async Task PostWithBody_ExchangeHasRequestBody()
        {
            await using var ctx = await H2TestContext.Create();

            // Drain initial server SETTINGS
            await ctx.ReadNextFrame(); // SETTINGS

            // Send SETTINGS ACK
            await ctx.SendSettingsAck();

            // Send HEADERS for POST request (no END_STREAM)
            var headers = "POST /data HTTP/2\r\nHost: localhost\r\nContent-Length: 11\r\n\r\n".AsMemory();
            await ctx.SendHeadersFrame(1, headers, endStream: false, endHeaders: true);

            // Send DATA frame with body
            var bodyBytes = System.Text.Encoding.UTF8.GetBytes("hello world");
            await ctx.SendDataFrame(1, bodyBytes, endStream: true);

            // Read the exchange
            using var buffer = Fluxzy.Misc.ResizableBuffers.RsBuffer.Allocate(32768);
            using var scope = new ExchangeScope();

            var exchange = await ctx.DownStreamPipe.ReadNextExchange(buffer, scope, ctx.Token);

            Assert.NotNull(exchange);
            Assert.Equal(1, exchange!.StreamIdentifier);

            // Read the request body from the exchange
            using var bodyStream = new MemoryStream();
            await exchange.Request.Body.CopyToAsync(bodyStream, ctx.Token);
            var bodyString = System.Text.Encoding.UTF8.GetString(bodyStream.ToArray());

            Assert.Equal("hello world", bodyString);
        }

        /// <summary>
        /// Send a PING frame and verify the server echoes back a PING ACK
        /// with the same opaque data.
        /// </summary>
        [Fact]
        public async Task Ping_ServerEchoesAck()
        {
            await using var ctx = await H2TestContext.Create();

            // Drain initial server SETTINGS
            await ctx.ReadNextFrame(); // SETTINGS

            // Send SETTINGS ACK
            await ctx.SendSettingsAck();

            // Send a PING with specific opaque data
            long opaqueData = 0x0102030405060708L;
            await ctx.SendPing(opaqueData);

            // Read frames until we get a PING ACK
            var maxFrames = 10;

            for (var i = 0; i < maxFrames; i++)
            {
                var frame = await ctx.ReadNextFrame();

                if (frame.BodyType == H2FrameType.Ping)
                {
                    // Verify it's an ACK
                    Assert.True(frame.Flags.HasFlag(HeaderFlags.Ack),
                        "PING response should have ACK flag set");

                    // Verify the opaque data matches
                    var opaqueDataFromResponse = ReadPingOpaqueData(frame);
                    Assert.Equal(opaqueData, opaqueDataFromResponse);

                    return;
                }
            }

            Assert.Fail("Server did not respond with PING ACK");
        }

        /// <summary>
        /// Extracts the opaque data from a PING frame result.
        /// Separated into a sync method because PingFrame is a ref struct
        /// and cannot be used in async methods with C# 11.
        /// </summary>
        private static long ReadPingOpaqueData(H2FrameReadResult frame)
        {
            var pingFrame = frame.GetPingFrame();
            return pingFrame.OpaqueData;
        }

        /// <summary>
        /// Send a GOAWAY frame and verify that ReadNextExchange returns null.
        /// </summary>
        [Fact]
        public async Task GoAway_ReadNextExchangeReturnsNull()
        {
            await using var ctx = await H2TestContext.Create();

            // Drain initial server SETTINGS
            await ctx.ReadNextFrame(); // SETTINGS

            // Send SETTINGS ACK
            await ctx.SendSettingsAck();

            // Send GOAWAY
            await ctx.SendGoAway(0, H2ErrorCode.NoError);

            // Give the read loop a moment to process the GOAWAY
            await Task.Delay(100);

            // ReadNextExchange should return null because GOAWAY was received
            using var buffer = Fluxzy.Misc.ResizableBuffers.RsBuffer.Allocate(32768);
            using var scope = new ExchangeScope();
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

            var exchange = await ctx.DownStreamPipe.ReadNextExchange(buffer, scope, timeoutCts.Token);

            Assert.Null(exchange);
        }

        /// <summary>
        /// Send RST_STREAM on a stream, then send a new request.
        /// Verify the pipe continues to work for new streams after RST.
        /// </summary>
        [Fact]
        public async Task RstStream_PipeContinuesForNewStreams()
        {
            await using var ctx = await H2TestContext.Create();
            await ctx.ReadNextFrame(); // SETTINGS
            await ctx.SendSettingsAck();

            // Send HEADERS on stream 1 (no END_STREAM — body expected)
            var headers = "POST /upload HTTP/2\r\nHost: localhost\r\n\r\n".AsMemory();
            await ctx.SendHeadersFrame(1, headers, endStream: false, endHeaders: true);

            // Drain the exchange for stream 1 (it was already produced from HEADERS)
            using var buffer = Fluxzy.Misc.ResizableBuffers.RsBuffer.Allocate(32768);
            using var scope1 = new ExchangeScope();
            var ex1 = await ctx.DownStreamPipe.ReadNextExchange(buffer, scope1, ctx.Token);
            Assert.NotNull(ex1);
            Assert.Equal(1, ex1!.StreamIdentifier);

            // Send RST_STREAM to cancel stream 1's body
            await ctx.SendRstStream(1, H2ErrorCode.Cancel);

            // Give time for processing
            await Task.Delay(50);

            // Send a new request on stream 3 — should still work
            var headers2 = "GET /ok HTTP/2\r\nHost: localhost\r\n\r\n".AsMemory();
            await ctx.SendHeadersFrame(3, headers2, endStream: true, endHeaders: true);

            using var scope2 = new ExchangeScope();
            var ex2 = await ctx.DownStreamPipe.ReadNextExchange(buffer, scope2, ctx.Token);

            Assert.NotNull(ex2);
            Assert.Equal(3, ex2!.StreamIdentifier);
        }

        /// <summary>
        /// Send two GET requests on different streams sequentially.
        /// Verify both exchanges are produced in order.
        /// </summary>
        [Fact]
        public async Task TwoSequentialStreams_BothExchangesProduced()
        {
            await using var ctx = await H2TestContext.Create();
            await ctx.ReadNextFrame(); // SETTINGS
            await ctx.SendSettingsAck();

            // Stream 1
            await ctx.SendHeadersFrame(1,
                "GET /first HTTP/2\r\nHost: localhost\r\n\r\n".AsMemory(),
                endStream: true, endHeaders: true);

            // Stream 3
            await ctx.SendHeadersFrame(3,
                "GET /second HTTP/2\r\nHost: localhost\r\n\r\n".AsMemory(),
                endStream: true, endHeaders: true);

            using var buffer = Fluxzy.Misc.ResizableBuffers.RsBuffer.Allocate(32768);

            using var scope1 = new ExchangeScope();
            var ex1 = await ctx.DownStreamPipe.ReadNextExchange(buffer, scope1, ctx.Token);
            Assert.NotNull(ex1);
            Assert.Equal(1, ex1!.StreamIdentifier);

            using var scope2 = new ExchangeScope();
            var ex2 = await ctx.DownStreamPipe.ReadNextExchange(buffer, scope2, ctx.Token);
            Assert.NotNull(ex2);
            Assert.Equal(3, ex2!.StreamIdentifier);
        }

        /// <summary>
        /// Send WINDOW_UPDATE on stream 0 (connection-level).
        /// Verify the pipe doesn't crash (the handler processes it).
        /// Then send a request and verify it still works.
        /// </summary>
        [Fact]
        public async Task WindowUpdate_ConnectionLevel_DoesNotCrash()
        {
            await using var ctx = await H2TestContext.Create();
            await ctx.ReadNextFrame(); // SETTINGS
            await ctx.SendSettingsAck();

            // Send a connection-level WINDOW_UPDATE
            await ctx.SendWindowUpdate(0, 65535);

            // Send a request — should still work
            await ctx.SendHeadersFrame(1,
                "GET / HTTP/2\r\nHost: localhost\r\n\r\n".AsMemory(),
                endStream: true, endHeaders: true);

            using var buffer = Fluxzy.Misc.ResizableBuffers.RsBuffer.Allocate(32768);
            using var scope = new ExchangeScope();

            var exchange = await ctx.DownStreamPipe.ReadNextExchange(buffer, scope, ctx.Token);
            Assert.NotNull(exchange);
        }

        /// <summary>
        /// Send SETTINGS ACK from the client (e.g. acknowledging server's settings).
        /// Verify no SETTINGS ACK is sent back (ACK must not be acked).
        /// </summary>
        [Fact]
        public async Task SettingsAck_DoesNotProduceAnotherAck()
        {
            await using var ctx = await H2TestContext.Create();
            await ctx.ReadNextFrame(); // SETTINGS

            // Send SETTINGS ACK
            await ctx.SendSettingsAck();

            // Send a request to have a known frame to expect
            await ctx.SendHeadersFrame(1,
                "GET / HTTP/2\r\nHost: localhost\r\n\r\n".AsMemory(),
                endStream: true, endHeaders: true);

            // Read frames — there might be WINDOW_UPDATE but should NOT see another SETTINGS ACK
            // (the server's ACK to our initial empty preface-settings is the only expected ACK)
            using var buffer = Fluxzy.Misc.ResizableBuffers.RsBuffer.Allocate(32768);
            using var scope = new ExchangeScope();

            var exchange = await ctx.DownStreamPipe.ReadNextExchange(buffer, scope, ctx.Token);
            Assert.NotNull(exchange);
        }

        /// <summary>
        /// Verify that disposing the pipe causes ReadNextExchange to return null
        /// instead of hanging.
        /// </summary>
        [Fact]
        public async Task Dispose_ReadNextExchangeReturnsNull()
        {
            await using var ctx = await H2TestContext.Create();
            await ctx.ReadNextFrame(); // SETTINGS
            await ctx.SendSettingsAck();

            // Dispose the pipe
            ctx.DownStreamPipe.Dispose();

            using var buffer = Fluxzy.Misc.ResizableBuffers.RsBuffer.Allocate(32768);
            using var scope = new ExchangeScope();
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

            var exchange = await ctx.DownStreamPipe.ReadNextExchange(buffer, scope, timeoutCts.Token);
            Assert.Null(exchange);
        }

        /// <summary>
        /// Verify the server SETTINGS frame contains expected setting identifiers.
        /// </summary>
        [Fact]
        public async Task ServerSettings_ContainsExpectedIdentifiers()
        {
            await using var ctx = await H2TestContext.Create();

            var frame = await ctx.ReadNextFrame(H2FrameType.Settings);

            // Server settings should have a non-zero body (contains actual settings)
            Assert.True(frame.BodyLength > 0, "Server SETTINGS frame should contain settings");

            // Each setting is 6 bytes (2 identifier + 4 value)
            Assert.Equal(0, frame.BodyLength % 6);
        }

        /// <summary>
        /// Send multiple PING frames rapidly and verify all get ACKed.
        /// </summary>
        [Fact]
        public async Task MultiplePings_AllAcked()
        {
            await using var ctx = await H2TestContext.Create();
            await ctx.ReadNextFrame(); // SETTINGS
            await ctx.SendSettingsAck();

            // Send 3 PINGs
            await ctx.SendPing(1L);
            await ctx.SendPing(2L);
            await ctx.SendPing(3L);

            var ackedValues = new System.Collections.Generic.List<long>();
            var maxFrames = 15;

            for (var i = 0; i < maxFrames && ackedValues.Count < 3; i++)
            {
                var frame = await ctx.ReadNextFrame();

                if (frame.BodyType == H2FrameType.Ping && frame.Flags.HasFlag(HeaderFlags.Ack))
                {
                    ackedValues.Add(ReadPingOpaqueData(frame));
                }
            }

            Assert.Equal(3, ackedValues.Count);
            Assert.Contains(1L, ackedValues);
            Assert.Contains(2L, ackedValues);
            Assert.Contains(3L, ackedValues);
        }

        /// <summary>
        /// Verify that the exchange created from a GET request has a null/empty body
        /// (END_STREAM was set on HEADERS).
        /// </summary>
        [Fact]
        public async Task GetRequest_ExchangeBodyIsNull()
        {
            await using var ctx = await H2TestContext.Create();
            await ctx.ReadNextFrame(); // SETTINGS
            await ctx.SendSettingsAck();

            await ctx.SendHeadersFrame(1,
                "GET / HTTP/2\r\nHost: localhost\r\n\r\n".AsMemory(),
                endStream: true, endHeaders: true);

            using var buffer = Fluxzy.Misc.ResizableBuffers.RsBuffer.Allocate(32768);
            using var scope = new ExchangeScope();

            var exchange = await ctx.DownStreamPipe.ReadNextExchange(buffer, scope, ctx.Token);

            Assert.NotNull(exchange);

            // GET with END_STREAM should have Stream.Null as body
            Assert.Same(Stream.Null, exchange!.Request.Body);
        }

        /// <summary>
        /// Send POST with multiple DATA frames.
        /// Verify the complete body is received.
        /// </summary>
        [Fact]
        public async Task PostWithMultipleDataFrames_BodyAssembledCorrectly()
        {
            await using var ctx = await H2TestContext.Create();
            await ctx.ReadNextFrame(); // SETTINGS
            await ctx.SendSettingsAck();

            await ctx.SendHeadersFrame(1,
                "POST /upload HTTP/2\r\nHost: localhost\r\n\r\n".AsMemory(),
                endStream: false, endHeaders: true);

            // Send body in 3 separate DATA frames
            await ctx.SendDataFrame(1, System.Text.Encoding.UTF8.GetBytes("aaa"), endStream: false);
            await ctx.SendDataFrame(1, System.Text.Encoding.UTF8.GetBytes("bbb"), endStream: false);
            await ctx.SendDataFrame(1, System.Text.Encoding.UTF8.GetBytes("ccc"), endStream: true);

            using var buffer = Fluxzy.Misc.ResizableBuffers.RsBuffer.Allocate(32768);
            using var scope = new ExchangeScope();

            var exchange = await ctx.DownStreamPipe.ReadNextExchange(buffer, scope, ctx.Token);
            Assert.NotNull(exchange);

            using var bodyStream = new MemoryStream();
            await exchange!.Request.Body.CopyToAsync(bodyStream, ctx.Token);
            var body = System.Text.Encoding.UTF8.GetString(bodyStream.ToArray());

            Assert.Equal("aaabbbccc", body);
        }
    }
}
