// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients.H2;
using Fluxzy.Clients.H2.Encoder;
using Fluxzy.Clients.H2.Encoder.Utils;
using Fluxzy.Clients.H2.Frames;
using Fluxzy.Core;

using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.UnitTests.H2Serve
{
    public class H2TrailerTests
    {
        /// <summary>
        /// Send POST with body then trailing HEADERS with EndStream.
        /// Verify exchange.Request.Trailers is populated with the correct fields.
        /// </summary>
        [Fact]
        public async Task PostWithTrailers_RequestTrailersPopulated()
        {
            await using var ctx = await H2TestContext.Create();
            await ctx.ReadNextFrame(); // SETTINGS
            await ctx.SendSettingsAck();

            // Send HEADERS for POST (no END_STREAM — body follows)
            var headers = "POST /upload HTTP/2\r\nHost: localhost\r\nContent-Type: application/grpc\r\n\r\n".AsMemory();
            await ctx.SendHeadersFrame(1, headers, endStream: false, endHeaders: true);

            // Send DATA frame with body
            var bodyBytes = Encoding.UTF8.GetBytes("request body");
            await ctx.SendDataFrame(1, bodyBytes, endStream: false);

            // Send trailing HEADERS with EndStream
            var trailerFields = new List<HeaderField>
            {
                new("grpc-status", "0"),
                new("grpc-message", "OK")
            };

            await ctx.SendTrailerHeadersFrame(1, trailerFields);

            // Read the exchange
            using var buffer = Fluxzy.Misc.ResizableBuffers.RsBuffer.Allocate(32768);
            using var scope = new ExchangeScope();
            var exchange = await ctx.DownStreamPipe.ReadNextExchange(buffer, scope, ctx.Token);

            Assert.NotNull(exchange);
            Assert.Equal(1, exchange!.StreamIdentifier);

            // Read request body
            using var bodyStream = new MemoryStream();
            await exchange.Request.Body!.CopyToAsync(bodyStream, ctx.Token);
            Assert.Equal("request body", Encoding.UTF8.GetString(bodyStream.ToArray()));

            // Give a small delay for trailer processing
            await Task.Delay(50);

            // Verify request trailers
            Assert.NotNull(exchange.Request.Trailers);
            Assert.Equal(2, exchange.Request.Trailers!.Count);

            var grpcStatus = exchange.Request.Trailers.FirstOrDefault(
                t => t.Name.Span.Equals("grpc-status", StringComparison.OrdinalIgnoreCase));
            Assert.Equal("0", grpcStatus.Value.ToString());

            var grpcMessage = exchange.Request.Trailers.FirstOrDefault(
                t => t.Name.Span.Equals("grpc-message", StringComparison.OrdinalIgnoreCase));
            Assert.Equal("OK", grpcMessage.Value.ToString());
        }

        /// <summary>
        /// Send POST with body and EndStream on the last DATA frame (no trailers).
        /// Verify exchange.Request.Trailers is null.
        /// </summary>
        [Fact]
        public async Task PostWithoutTrailers_RequestTrailersNull()
        {
            await using var ctx = await H2TestContext.Create();
            await ctx.ReadNextFrame(); // SETTINGS
            await ctx.SendSettingsAck();

            var headers = "POST /upload HTTP/2\r\nHost: localhost\r\n\r\n".AsMemory();
            await ctx.SendHeadersFrame(1, headers, endStream: false, endHeaders: true);

            var bodyBytes = Encoding.UTF8.GetBytes("just body");
            await ctx.SendDataFrame(1, bodyBytes, endStream: true);

            using var buffer = Fluxzy.Misc.ResizableBuffers.RsBuffer.Allocate(32768);
            using var scope = new ExchangeScope();
            var exchange = await ctx.DownStreamPipe.ReadNextExchange(buffer, scope, ctx.Token);

            Assert.NotNull(exchange);

            // Read body to completion
            using var bodyStream = new MemoryStream();
            await exchange!.Request.Body!.CopyToAsync(bodyStream, ctx.Token);
            Assert.Equal("just body", Encoding.UTF8.GetString(bodyStream.ToArray()));

            // No trailers sent, so should be null
            Assert.Null(exchange.Request.Trailers);
        }

        /// <summary>
        /// GET request with EndStream on HEADERS — no body, no trailers.
        /// Verify exchange.Request.Trailers is null.
        /// </summary>
        [Fact]
        public async Task GetWithEndStream_RequestTrailersNull()
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
            Assert.Null(exchange!.Request.Trailers);
        }

        /// <summary>
        /// POST with multiple DATA frames then trailers.
        /// Verifies body is assembled correctly and trailers are captured.
        /// </summary>
        [Fact]
        public async Task PostMultipleDataFramesThenTrailers_AllCaptured()
        {
            await using var ctx = await H2TestContext.Create();
            await ctx.ReadNextFrame(); // SETTINGS
            await ctx.SendSettingsAck();

            var headers = "POST /stream HTTP/2\r\nHost: localhost\r\n\r\n".AsMemory();
            await ctx.SendHeadersFrame(1, headers, endStream: false, endHeaders: true);

            // Send body in 3 separate DATA frames (none with EndStream)
            await ctx.SendDataFrame(1, Encoding.UTF8.GetBytes("chunk1"), endStream: false);
            await ctx.SendDataFrame(1, Encoding.UTF8.GetBytes("chunk2"), endStream: false);
            await ctx.SendDataFrame(1, Encoding.UTF8.GetBytes("chunk3"), endStream: false);

            // End stream with trailers
            var trailerFields = new List<HeaderField>
            {
                new("checksum", "abc123")
            };
            await ctx.SendTrailerHeadersFrame(1, trailerFields);

            using var buffer = Fluxzy.Misc.ResizableBuffers.RsBuffer.Allocate(32768);
            using var scope = new ExchangeScope();
            var exchange = await ctx.DownStreamPipe.ReadNextExchange(buffer, scope, ctx.Token);

            Assert.NotNull(exchange);

            // Read body
            using var bodyStream = new MemoryStream();
            await exchange!.Request.Body!.CopyToAsync(bodyStream, ctx.Token);
            Assert.Equal("chunk1chunk2chunk3", Encoding.UTF8.GetString(bodyStream.ToArray()));

            await Task.Delay(50);

            // Verify trailer
            Assert.NotNull(exchange.Request.Trailers);
            Assert.Single(exchange.Request.Trailers!);
            Assert.Equal("checksum", exchange.Request.Trailers[0].Name.ToString());
            Assert.Equal("abc123", exchange.Request.Trailers[0].Value.ToString());
        }

        /// <summary>
        /// POST with trailers containing many fields.
        /// Verifies all trailer fields are captured.
        /// </summary>
        [Fact]
        public async Task PostWithManyTrailerFields_AllFieldsCaptured()
        {
            await using var ctx = await H2TestContext.Create();
            await ctx.ReadNextFrame(); // SETTINGS
            await ctx.SendSettingsAck();

            var headers = "POST /multi HTTP/2\r\nHost: localhost\r\n\r\n".AsMemory();
            await ctx.SendHeadersFrame(1, headers, endStream: false, endHeaders: true);

            await ctx.SendDataFrame(1, Encoding.UTF8.GetBytes("data"), endStream: false);

            var trailerFields = new List<HeaderField>
            {
                new("x-field-1", "value1"),
                new("x-field-2", "value2"),
                new("x-field-3", "value3"),
                new("x-field-4", "value4"),
                new("x-field-5", "value5")
            };
            await ctx.SendTrailerHeadersFrame(1, trailerFields);

            using var buffer = Fluxzy.Misc.ResizableBuffers.RsBuffer.Allocate(32768);
            using var scope = new ExchangeScope();
            var exchange = await ctx.DownStreamPipe.ReadNextExchange(buffer, scope, ctx.Token);

            Assert.NotNull(exchange);

            using var bodyStream = new MemoryStream();
            await exchange!.Request.Body!.CopyToAsync(bodyStream, ctx.Token);

            await Task.Delay(50);

            Assert.NotNull(exchange.Request.Trailers);
            Assert.Equal(5, exchange.Request.Trailers!.Count);

            for (var i = 0; i < 5; i++)
            {
                var field = exchange.Request.Trailers.First(
                    t => t.Name.Span.Equals($"x-field-{i + 1}", StringComparison.OrdinalIgnoreCase));
                Assert.Equal($"value{i + 1}", field.Value.ToString());
            }
        }

        /// <summary>
        /// POST with trailers on one stream followed by a clean GET on another stream.
        /// Verifies the pipe continues to work after processing trailers.
        /// </summary>
        [Fact]
        public async Task TrailersOnStream1_ThenGetOnStream3_BothWork()
        {
            await using var ctx = await H2TestContext.Create();
            await ctx.ReadNextFrame(); // SETTINGS
            await ctx.SendSettingsAck();

            // Stream 1: POST with trailers
            await ctx.SendHeadersFrame(1,
                "POST /first HTTP/2\r\nHost: localhost\r\n\r\n".AsMemory(),
                endStream: false, endHeaders: true);

            await ctx.SendDataFrame(1, Encoding.UTF8.GetBytes("body1"), endStream: false);
            await ctx.SendTrailerHeadersFrame(1, new List<HeaderField>
            {
                new("x-trailer", "stream1")
            });

            // Stream 3: simple GET
            await ctx.SendHeadersFrame(3,
                "GET /second HTTP/2\r\nHost: localhost\r\n\r\n".AsMemory(),
                endStream: true, endHeaders: true);

            using var buffer = Fluxzy.Misc.ResizableBuffers.RsBuffer.Allocate(32768);

            // Read exchange 1
            using var scope1 = new ExchangeScope();
            var ex1 = await ctx.DownStreamPipe.ReadNextExchange(buffer, scope1, ctx.Token);
            Assert.NotNull(ex1);
            Assert.Equal(1, ex1!.StreamIdentifier);

            // Drain body
            using var body1 = new MemoryStream();
            await ex1.Request.Body!.CopyToAsync(body1, ctx.Token);

            await Task.Delay(50);

            Assert.NotNull(ex1.Request.Trailers);
            Assert.Equal("x-trailer", ex1.Request.Trailers![0].Name.ToString());

            // Read exchange 2
            using var scope2 = new ExchangeScope();
            var ex2 = await ctx.DownStreamPipe.ReadNextExchange(buffer, scope2, ctx.Token);
            Assert.NotNull(ex2);
            Assert.Equal(3, ex2!.StreamIdentifier);
            Assert.Null(ex2.Request.Trailers);
        }

        /// <summary>
        /// POST with body ending via DATA EndStream and a separate POST with trailers.
        /// Verifies the first has no trailers and the second has trailers.
        /// </summary>
        [Fact]
        public async Task MixedStreams_WithAndWithoutTrailers()
        {
            await using var ctx = await H2TestContext.Create();
            await ctx.ReadNextFrame(); // SETTINGS
            await ctx.SendSettingsAck();

            // Stream 1: POST without trailers (EndStream on DATA)
            await ctx.SendHeadersFrame(1,
                "POST /no-trailers HTTP/2\r\nHost: localhost\r\n\r\n".AsMemory(),
                endStream: false, endHeaders: true);
            await ctx.SendDataFrame(1, Encoding.UTF8.GetBytes("body-no-trailers"), endStream: true);

            // Stream 3: POST with trailers
            await ctx.SendHeadersFrame(3,
                "POST /with-trailers HTTP/2\r\nHost: localhost\r\n\r\n".AsMemory(),
                endStream: false, endHeaders: true);
            await ctx.SendDataFrame(3, Encoding.UTF8.GetBytes("body-with-trailers"), endStream: false);
            await ctx.SendTrailerHeadersFrame(3, new List<HeaderField>
            {
                new("x-checksum", "deadbeef")
            });

            using var buffer = Fluxzy.Misc.ResizableBuffers.RsBuffer.Allocate(32768);

            // Exchange 1
            using var scope1 = new ExchangeScope();
            var ex1 = await ctx.DownStreamPipe.ReadNextExchange(buffer, scope1, ctx.Token);
            Assert.NotNull(ex1);

            using var body1 = new MemoryStream();
            await ex1!.Request.Body!.CopyToAsync(body1, ctx.Token);
            Assert.Equal("body-no-trailers", Encoding.UTF8.GetString(body1.ToArray()));
            Assert.Null(ex1.Request.Trailers);

            // Exchange 2
            using var scope2 = new ExchangeScope();
            var ex2 = await ctx.DownStreamPipe.ReadNextExchange(buffer, scope2, ctx.Token);
            Assert.NotNull(ex2);

            using var body2 = new MemoryStream();
            await ex2!.Request.Body!.CopyToAsync(body2, ctx.Token);
            Assert.Equal("body-with-trailers", Encoding.UTF8.GetString(body2.ToArray()));

            await Task.Delay(50);

            Assert.NotNull(ex2.Request.Trailers);
            Assert.Single(ex2.Request.Trailers!);
            Assert.Equal("x-checksum", ex2.Request.Trailers[0].Name.ToString());
            Assert.Equal("deadbeef", ex2.Request.Trailers[0].Value.ToString());
        }
    }

    public class H2TrailerEncodingTests
    {
        /// <summary>
        /// HPackEncoder.EncodeFields + HPackDecoder.DecodeTrailerFields round-trip.
        /// </summary>
        [Fact]
        public void EncodeFields_DecodeTrailerFields_RoundTrip()
        {
            var memoryProvider = ArrayPoolMemoryProvider<char>.Default;
            var encoder = new HPackEncoder(new EncodingContext(memoryProvider));
            var decoder = new HPackDecoder(
                new DecodingContext(new Authority("localhost", 443, true), memoryProvider));

            var original = new List<HeaderField>
            {
                new("grpc-status", "0"),
                new("grpc-message", "Success"),
                new("x-custom-trailer", "custom-value")
            };

            // Encode
            var buffer = new byte[4096];
            var encoded = encoder.EncodeFields(original, buffer);

            // Decode
            var decoded = decoder.DecodeTrailerFields(encoded);

            Assert.Equal(original.Count, decoded.Count);

            for (var i = 0; i < original.Count; i++)
            {
                Assert.Equal(
                    original[i].Name.ToString().ToLowerInvariant(),
                    decoded[i].Name.ToString().ToLowerInvariant());
                Assert.Equal(original[i].Value.ToString(), decoded[i].Value.ToString());
            }
        }

        /// <summary>
        /// Verify DecodeTrailerFields returns empty list for empty input.
        /// </summary>
        [Fact]
        public void DecodeTrailerFields_EmptyInput_ReturnsEmptyList()
        {
            var memoryProvider = ArrayPoolMemoryProvider<char>.Default;
            var decoder = new HPackDecoder(
                new DecodingContext(new Authority("localhost", 443, true), memoryProvider));

            var result = decoder.DecodeTrailerFields(ReadOnlySpan<byte>.Empty);

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        /// <summary>
        /// HeaderEncoder.EncodeTrailers produces valid packetized frames.
        /// </summary>
        [Fact]
        public void HeaderEncoder_EncodeTrailers_ProducesValidFrames()
        {
            var setting = new H2StreamSetting();
            var memoryProvider = ArrayPoolMemoryProvider<char>.Default;
            var encoder = new HeaderEncoder(
                new HPackEncoder(new EncodingContext(memoryProvider)),
                new HPackDecoder(new DecodingContext(new Authority("localhost", 443, true), memoryProvider)),
                setting);

            var trailers = new List<HeaderField>
            {
                new("grpc-status", "0"),
                new("grpc-message", "OK")
            };

            using var buffer = Fluxzy.Misc.ResizableBuffers.RsBuffer.Allocate(4096);
            var result = encoder.EncodeTrailers(trailers, buffer, streamIdentifier: 1);

            // Should produce at least one HEADERS frame (9-byte header + payload)
            Assert.True(result.Length >= 9, "Encoded trailers should produce at least a 9-byte frame header");

            // Parse the frame header
            var frameHeader = new H2Frame(result.Span.Slice(0, 9));
            Assert.Equal(H2FrameType.Headers, frameHeader.BodyType);
            Assert.Equal(1, frameHeader.StreamIdentifier);
            Assert.True(frameHeader.Flags.HasFlag(HeaderFlags.EndStream),
                "Trailer HEADERS frame must have EndStream flag");
        }
    }

    public class H2TrailerSerializationTests
    {
        /// <summary>
        /// ExchangeInfo preserves request trailers through construction from Exchange.
        /// </summary>
        [Fact]
        public void ExchangeInfo_PreservesRequestTrailers()
        {
            var authority = new Authority("localhost", 443, true);
            var idProvider = new TestIdProvider();
            var requestHeader = new RequestHeader("GET / HTTP/1.1\r\nHost: localhost\r\n\r\n".AsMemory(), true);
            var exchange = new Exchange(idProvider, authority, "GET / HTTP/1.1\r\nHost: localhost\r\n\r\n".AsMemory(), "HTTP/2", DateTime.UtcNow);

            exchange.Request.Trailers = new List<HeaderField>
            {
                new("grpc-status", "0"),
                new("grpc-message", "OK")
            };

            var info = new ExchangeInfo(exchange);

            Assert.NotNull(info.RequestTrailers);
            Assert.Equal(2, info.RequestTrailers!.Count);
            Assert.Equal("grpc-status", info.RequestTrailers[0].Name.ToString());
            Assert.Equal("0", info.RequestTrailers[0].Value.ToString());
            Assert.Equal("grpc-message", info.RequestTrailers[1].Name.ToString());
            Assert.Equal("OK", info.RequestTrailers[1].Value.ToString());
        }

        /// <summary>
        /// ExchangeInfo preserves response trailers through construction from Exchange.
        /// </summary>
        [Fact]
        public void ExchangeInfo_PreservesResponseTrailers()
        {
            var authority = new Authority("localhost", 443, true);
            var idProvider = new TestIdProvider();
            var exchange = new Exchange(idProvider, authority, "GET / HTTP/1.1\r\nHost: localhost\r\n\r\n".AsMemory(), "HTTP/2", DateTime.UtcNow);

            exchange.Response.Header = new ResponseHeader("HTTP/1.1 200 OK\r\n\r\n".AsMemory(), true, false);
            exchange.Response.Trailers = new List<HeaderField>
            {
                new("x-server-timing", "total;dur=123")
            };

            // Force completion so ExchangeInfo constructor can read Pending
            exchange.ExchangeCompletionSource.TrySetResult(false);

            var info = new ExchangeInfo(exchange);

            Assert.NotNull(info.ResponseTrailers);
            Assert.Single(info.ResponseTrailers!);
            Assert.Equal("x-server-timing", info.ResponseTrailers[0].Name.ToString());
            Assert.Equal("total;dur=123", info.ResponseTrailers[0].Value.ToString());
        }

        /// <summary>
        /// ExchangeInfo has null trailers when Exchange has no trailers.
        /// </summary>
        [Fact]
        public void ExchangeInfo_NullTrailersWhenExchangeHasNone()
        {
            var authority = new Authority("localhost", 443, true);
            var idProvider = new TestIdProvider();
            var exchange = new Exchange(idProvider, authority, "GET / HTTP/1.1\r\nHost: localhost\r\n\r\n".AsMemory(), "HTTP/2", DateTime.UtcNow);

            exchange.ExchangeCompletionSource.TrySetResult(false);

            var info = new ExchangeInfo(exchange);

            Assert.Null(info.RequestTrailers);
            Assert.Null(info.ResponseTrailers);
        }

        /// <summary>
        /// IExchange.GetRequestTrailers() returns correct values from Exchange.
        /// </summary>
        [Fact]
        public void IExchange_GetRequestTrailers_ReturnsCorrectValues()
        {
            var authority = new Authority("localhost", 443, true);
            var idProvider = new TestIdProvider();
            var exchange = new Exchange(idProvider, authority, "GET / HTTP/1.1\r\nHost: localhost\r\n\r\n".AsMemory(), "HTTP/2", DateTime.UtcNow);

            exchange.Request.Trailers = new List<HeaderField>
            {
                new("grpc-status", "12")
            };

            IExchange iex = exchange;
            var trailers = iex.GetRequestTrailers()?.ToList();

            Assert.NotNull(trailers);
            Assert.Single(trailers!);
            Assert.Equal("grpc-status", trailers[0].Name.ToString());
            Assert.Equal("12", trailers[0].Value.ToString());
        }

        /// <summary>
        /// IExchange.GetResponseTrailers() returns null when no trailers set.
        /// </summary>
        [Fact]
        public void IExchange_GetResponseTrailers_ReturnsNullWhenNotSet()
        {
            var authority = new Authority("localhost", 443, true);
            var idProvider = new TestIdProvider();
            var exchange = new Exchange(idProvider, authority, "GET / HTTP/1.1\r\nHost: localhost\r\n\r\n".AsMemory(), "HTTP/2", DateTime.UtcNow);

            IExchange iex = exchange;

            Assert.Null(iex.GetRequestTrailers());
            Assert.Null(iex.GetResponseTrailers());
        }

        /// <summary>
        /// IExchange.GetRequestTrailers/GetResponseTrailers on ExchangeInfo returns correct data.
        /// </summary>
        [Fact]
        public void IExchange_ExchangeInfo_TrailerMethodsWork()
        {
            var authority = new Authority("localhost", 443, true);
            var idProvider = new TestIdProvider();
            var exchange = new Exchange(idProvider, authority, "GET / HTTP/1.1\r\nHost: localhost\r\n\r\n".AsMemory(), "HTTP/2", DateTime.UtcNow);

            exchange.Request.Trailers = new List<HeaderField> { new("req-trailer", "rv") };
            exchange.Response.Header = new ResponseHeader("HTTP/1.1 200 OK\r\n\r\n".AsMemory(), true, false);
            exchange.Response.Trailers = new List<HeaderField> { new("res-trailer", "rv2") };
            exchange.ExchangeCompletionSource.TrySetResult(false);

            var info = new ExchangeInfo(exchange);
            IExchange iex = info;

            var reqTrailers = iex.GetRequestTrailers()?.ToList();
            Assert.NotNull(reqTrailers);
            Assert.Single(reqTrailers!);
            Assert.Equal("req-trailer", reqTrailers[0].Name.ToString());

            var resTrailers = iex.GetResponseTrailers()?.ToList();
            Assert.NotNull(resTrailers);
            Assert.Single(resTrailers!);
            Assert.Equal("res-trailer", resTrailers[0].Name.ToString());
        }
    }
}
