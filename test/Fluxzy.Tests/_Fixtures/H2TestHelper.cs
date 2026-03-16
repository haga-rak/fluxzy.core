// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Clients.H2;
using Fluxzy.Clients.H2.Encoder;
using Fluxzy.Clients.H2.Encoder.Utils;
using Fluxzy.Clients.H2.Frames;
using Fluxzy.Core;
using Fluxzy.Misc.ResizableBuffers;
using Fluxzy.Rules;

namespace Fluxzy.Tests._Fixtures
{
    /// <summary>
    /// Provides a pair of connected streams using System.IO.Pipelines.Pipe,
    /// simulating a bidirectional connection between a client and server.
    /// </summary>
    public class DuplexPipe : IDisposable
    {
        private readonly Pipe _clientToServer;
        private readonly Pipe _serverToClient;

        public DuplexPipe()
        {
            _clientToServer = new Pipe();
            _serverToClient = new Pipe();

            ClientWriteStream = _clientToServer.Writer.AsStream();
            ServerReadStream = _clientToServer.Reader.AsStream();

            ServerWriteStream = _serverToClient.Writer.AsStream();
            ClientReadStream = _serverToClient.Reader.AsStream();
        }

        public Stream ClientWriteStream { get; }
        public Stream ClientReadStream { get; }
        public Stream ServerReadStream { get; }
        public Stream ServerWriteStream { get; }

        public void Dispose()
        {
            _clientToServer.Writer.Complete();
            _clientToServer.Reader.Complete();
            _serverToClient.Writer.Complete();
            _serverToClient.Reader.Complete();
        }
    }

    /// <summary>
    /// A simple IIdProvider for testing that returns incrementing IDs.
    /// </summary>
    internal class TestIdProvider : IIdProvider
    {
        private int _nextExchange = 0;
        private int _nextConnection = 0;

        public int NextExchangeId()
        {
            return Interlocked.Increment(ref _nextExchange);
        }

        public int NextConnectionId()
        {
            return Interlocked.Increment(ref _nextConnection);
        }
    }

    /// <summary>
    /// A simple IExchangeContextBuilder for testing that returns a default ExchangeContext.
    /// </summary>
    internal class TestExchangeContextBuilder : IExchangeContextBuilder
    {
        public ValueTask<ExchangeContext> Create(Authority authority, bool secure)
        {
            var context = new ExchangeContext(
                authority,
                new VariableContext(),
                null,
                SetUserAgentActionMapping.Default)
            {
                Secure = secure
            };

            return new ValueTask<ExchangeContext>(context);
        }
    }

    /// <summary>
    /// Contains synchronous helpers for building H2 frame byte arrays.
    /// Separated from the async H2TestContext so that Span-based APIs
    /// can be used (C# 11 does not allow Span in async methods).
    /// </summary>
    internal static class H2FrameHelper
    {
        public static byte[] BuildSettingsFrame(params (SettingIdentifier id, int value)[] settings)
        {
            var buffer = new byte[512];
            var written = 0;
            var headerSize = 9;

            foreach (var (id, value) in settings)
            {
                written += SettingFrame.WriteMultipleBody(
                    buffer.AsSpan(written + headerSize), id, value);
            }

            written += SettingFrame.WriteMultipleHeader(buffer.AsSpan(), settings.Length);

            var result = new byte[written];
            Array.Copy(buffer, result, written);
            return result;
        }

        public static byte[] BuildSettingsAck()
        {
            var buffer = new byte[9];
            new SettingFrame(true).Write(buffer);
            return buffer;
        }

        public static byte[] BuildHeadersFrame(
            HPackEncoder encoder, int streamId,
            ReadOnlyMemory<char> plainHeaders,
            bool endStream, bool endHeaders)
        {
            var encodedBuffer = new byte[plainHeaders.Length * 4 + 256];
            var encoded = encoder.Encode(plainHeaders, encodedBuffer);

            HeaderFlags flags = HeaderFlags.None;

            if (endStream)
                flags |= HeaderFlags.EndStream;

            if (endHeaders)
                flags |= HeaderFlags.EndHeaders;

            var frameBuffer = new byte[9 + encoded.Length];
            H2Frame.Write(frameBuffer.AsSpan(), encoded.Length, H2FrameType.Headers, flags, streamId);
            encoded.CopyTo(frameBuffer.AsSpan(9));

            return frameBuffer;
        }

        public static byte[] BuildDataFrame(int streamId, byte[] data, bool endStream)
        {
            HeaderFlags flags = endStream ? HeaderFlags.EndStream : HeaderFlags.None;

            var frameBuffer = new byte[9 + data.Length];
            H2Frame.Write(frameBuffer.AsSpan(), data.Length, H2FrameType.Data, flags, streamId);
            Array.Copy(data, 0, frameBuffer, 9, data.Length);

            return frameBuffer;
        }

        public static byte[] BuildWindowUpdate(int streamId, int increment)
        {
            var buffer = new byte[9 + 4];
            new WindowUpdateFrame(increment, streamId).Write(buffer);
            return buffer;
        }

        public static byte[] BuildRstStream(int streamId, H2ErrorCode errorCode)
        {
            var buffer = new byte[9 + 4];
            new RstStreamFrame(streamId, errorCode).Write(buffer);
            return buffer;
        }

        public static byte[] BuildGoAway(int lastStreamId, H2ErrorCode errorCode)
        {
            var buffer = new byte[9 + 8];
            new GoAwayFrame(lastStreamId, errorCode).Write(buffer);
            return buffer;
        }

        public static byte[] BuildPing(long opaqueData)
        {
            var buffer = new byte[9 + 8];
            new PingFrame(opaqueData, HeaderFlags.None).Write(buffer);
            return buffer;
        }

        public static H2FrameReadResult ParseFrame(byte[] headerBuffer, byte[] bodyBuffer)
        {
            var frame = new H2Frame(headerBuffer.AsSpan());

            if (bodyBuffer.Length == 0)
                return new H2FrameReadResult(frame, ReadOnlyMemory<byte>.Empty);

            return new H2FrameReadResult(frame, bodyBuffer);
        }
    }

    /// <summary>
    /// Sets up everything needed to test H2DownStreamPipe with in-memory streams.
    /// Provides client-side helpers for sending H2 frames and server-side helpers
    /// for reading H2 frames written by the pipe.
    /// </summary>
    internal class H2TestContext : IAsyncDisposable
    {
        private readonly HPackEncoder _clientEncoder;
        private readonly CancellationTokenSource _cts;

        private H2TestContext(
            DuplexPipe pipe,
            H2DownStreamPipe downStreamPipe,
            HPackEncoder clientEncoder)
        {
            Pipe = pipe;
            DownStreamPipe = downStreamPipe;
            _clientEncoder = clientEncoder;
            _cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        }

        public DuplexPipe Pipe { get; }

        public H2DownStreamPipe DownStreamPipe { get; }

        public CancellationToken Token => _cts.Token;

        /// <summary>
        /// Creates a fully initialized H2TestContext:
        /// creates the pipe, creates H2DownStreamPipe, sends the client preface,
        /// and calls Init on the pipe.
        /// </summary>
        public static async Task<H2TestContext> Create()
        {
            var pipe = new DuplexPipe();
            var authority = new Authority("localhost", 443, true);

            var downStreamPipe = new H2DownStreamPipe(
                new TestIdProvider(),
                authority,
                pipe.ServerReadStream,
                pipe.ServerWriteStream,
                new TestExchangeContextBuilder());

            var memoryProvider = ArrayPoolMemoryProvider<char>.Default;
            var clientEncoder = new HPackEncoder(
                new EncodingContext(memoryProvider));

            var context = new H2TestContext(pipe, downStreamPipe, clientEncoder);

            // Send client connection preface
            await context.SendClientPreface();

            // Init the downstream pipe (it reads preface and sends server SETTINGS)
            using var initBuffer = RsBuffer.Allocate(128 * 1024 + 9);
            await downStreamPipe.Init(initBuffer);

            return context;
        }

        public async Task SendClientPreface()
        {
            await Pipe.ClientWriteStream.WriteAsync(H2Constants.Preface, Token);
            await Pipe.ClientWriteStream.FlushAsync(Token);
        }

        public async Task SendSettingsFrame(params (SettingIdentifier id, int value)[] settings)
        {
            var buffer = H2FrameHelper.BuildSettingsFrame(settings);
            await Pipe.ClientWriteStream.WriteAsync(buffer, Token);
            await Pipe.ClientWriteStream.FlushAsync(Token);
        }

        public async Task SendSettingsAck()
        {
            var buffer = H2FrameHelper.BuildSettingsAck();
            await Pipe.ClientWriteStream.WriteAsync(buffer, Token);
            await Pipe.ClientWriteStream.FlushAsync(Token);
        }

        public async Task SendHeadersFrame(
            int streamId, ReadOnlyMemory<char> plainHeaders,
            bool endStream, bool endHeaders)
        {
            var frameBuffer = H2FrameHelper.BuildHeadersFrame(
                _clientEncoder, streamId, plainHeaders, endStream, endHeaders);
            await Pipe.ClientWriteStream.WriteAsync(frameBuffer, Token);
            await Pipe.ClientWriteStream.FlushAsync(Token);
        }

        public async Task SendDataFrame(int streamId, byte[] data, bool endStream)
        {
            var frameBuffer = H2FrameHelper.BuildDataFrame(streamId, data, endStream);
            await Pipe.ClientWriteStream.WriteAsync(frameBuffer, Token);
            await Pipe.ClientWriteStream.FlushAsync(Token);
        }

        public async Task SendWindowUpdate(int streamId, int increment)
        {
            var buffer = H2FrameHelper.BuildWindowUpdate(streamId, increment);
            await Pipe.ClientWriteStream.WriteAsync(buffer, Token);
            await Pipe.ClientWriteStream.FlushAsync(Token);
        }

        public async Task SendRstStream(int streamId, H2ErrorCode errorCode)
        {
            var buffer = H2FrameHelper.BuildRstStream(streamId, errorCode);
            await Pipe.ClientWriteStream.WriteAsync(buffer, Token);
            await Pipe.ClientWriteStream.FlushAsync(Token);
        }

        public async Task SendGoAway(int lastStreamId, H2ErrorCode errorCode)
        {
            var buffer = H2FrameHelper.BuildGoAway(lastStreamId, errorCode);
            await Pipe.ClientWriteStream.WriteAsync(buffer, Token);
            await Pipe.ClientWriteStream.FlushAsync(Token);
        }

        public async Task SendPing(long opaqueData)
        {
            var buffer = H2FrameHelper.BuildPing(opaqueData);
            await Pipe.ClientWriteStream.WriteAsync(buffer, Token);
            await Pipe.ClientWriteStream.FlushAsync(Token);
        }

        /// <summary>
        /// Reads the next H2 frame from the server's write stream
        /// (i.e., what H2DownStreamPipe wrote back to the client).
        /// </summary>
        public async Task<H2FrameReadResult> ReadNextFrame()
        {
            var headerBuffer = new byte[9];
            await ReadExactAsync(Pipe.ClientReadStream, headerBuffer, Token);

            // Parse header synchronously to avoid Span-in-async issue
            var tempFrame = H2FrameHelper.ParseFrame(headerBuffer, Array.Empty<byte>());
            var bodyLength = tempFrame.BodyLength;

            if (bodyLength == 0)
                return tempFrame;

            var bodyBuffer = new byte[bodyLength];
            await ReadExactAsync(Pipe.ClientReadStream, bodyBuffer, Token);

            return H2FrameHelper.ParseFrame(headerBuffer, bodyBuffer);
        }

        /// <summary>
        /// Reads the next H2 frame, expecting a specific frame type.
        /// Throws if the frame type does not match.
        /// </summary>
        public async Task<H2FrameReadResult> ReadNextFrame(H2FrameType expectedType)
        {
            var result = await ReadNextFrame();

            if (result.BodyType != expectedType)
                throw new InvalidOperationException(
                    $"Expected frame type {expectedType} but got {result.BodyType}");

            return result;
        }

        /// <summary>
        /// Reads frames until a frame of the specified type is found.
        /// </summary>
        public async Task<H2FrameReadResult> ReadUntilFrameType(H2FrameType targetType)
        {
            while (true)
            {
                var frame = await ReadNextFrame();

                if (frame.BodyType == targetType)
                    return frame;
            }
        }

        /// <summary>
        /// Drains all initial server frames (SETTINGS, WINDOW_UPDATE)
        /// and sends the appropriate client-side responses.
        /// </summary>
        public async Task CompleteHandshake()
        {
            // Read server SETTINGS frame
            await ReadNextFrame(H2FrameType.Settings);

            // Send client SETTINGS (empty)
            await SendSettingsFrame();

            // Send SETTINGS ACK for the server's SETTINGS
            await SendSettingsAck();
        }

        private static async Task ReadExactAsync(Stream stream, byte[] buffer, CancellationToken token)
        {
            var offset = 0;
            var remaining = buffer.Length;

            while (remaining > 0)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(offset, remaining), token);

                if (read == 0)
                    throw new EndOfStreamException("Unexpected end of stream while reading H2 frame");

                offset += read;
                remaining -= read;
            }
        }

        public async ValueTask DisposeAsync()
        {
            DownStreamPipe.Dispose();
            Pipe.Dispose();
            _cts.Dispose();
        }
    }
}
