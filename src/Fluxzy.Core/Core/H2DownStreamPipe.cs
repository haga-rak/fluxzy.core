// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Clients.H2;
using Fluxzy.Clients.H2.Encoder;
using Fluxzy.Clients.H2.Encoder.Utils;
using Fluxzy.Clients.H2.Frames;
using Fluxzy.Misc.ResizableBuffers;
using Fluxzy.Misc.Streams;

namespace Fluxzy.Core
{
    /// <summary>
    ///     Represents a pooled buffer written to the write channel.
    ///     The WriteLoop returns the array to ArrayPool after writing.
    /// </summary>
    internal readonly struct PooledFrame
    {
        public readonly byte[] Array;
        public readonly int Length;
        public readonly bool Pooled;

        public PooledFrame(byte[] array, int length, bool pooled)
        {
            Array = array;
            Length = length;
            Pooled = pooled;
        }

        public ReadOnlyMemory<byte> Memory => new ReadOnlyMemory<byte>(Array, 0, Length);
    }

    internal class H2DownStreamPipe : IDownStreamPipe
    {
        private readonly Stream _readStream;
        private readonly Stream _writeStream;
        private readonly IIdProvider _idProvider;
        private readonly IExchangeContextBuilder _contextBuilder;

        private Task? _readLoop;
        private Task? _writeLoop;

        private readonly Channel<Exchange> _exchangeChannel =
            Channel.CreateUnbounded<Exchange>();

        private readonly Channel<PooledFrame> _writeChannel =
                        Channel.CreateUnbounded<PooledFrame>();

        private readonly ConcurrentDictionary<int, ServerStreamWorker> _currentStreams = new();
        private readonly HeaderEncoder _headerEncoder;
        private readonly SemaphoreSlim _headerEncodeLock = new SemaphoreSlim(1, 1);
        private readonly H2StreamSetting _h2StreamSetting = new H2StreamSetting();
        private readonly WindowSizeHolder _overallWindowSizeHolder;
        private readonly H2Logger _logger;
        private readonly CancellationToken _mainLoopToken;
        private readonly CancellationTokenSource _mainLoopTokenSource;

        // Pre-built static frame for empty end-stream DATA (9 bytes, reused across all streams after patching stream ID)
        // Not static because stream ID varies — but the 9-byte header is tiny, we use stackalloc + pool

        private int _unNotifiedWindowSize;
        private bool _readHalted;
        private bool _writeHalted;
        private int _lastStreamId = int.MaxValue;
        private bool _disposed;
        private bool _goAwayReceived;
        private H2ErrorCode _goAwayErrorCode;

        public H2DownStreamPipe(
            IIdProvider idProvider,
            Authority requestedAuthority, Stream readStream, Stream writeStream,
            IExchangeContextBuilder contextBuilder)
        {
            _readStream = readStream;
            _writeStream = writeStream;
            _idProvider = idProvider;
            _contextBuilder = contextBuilder;
            RequestedAuthority = requestedAuthority;

            var hPackEncoder =
                new HPackEncoder(new EncodingContext(ArrayPoolMemoryProvider<char>.Default));

            var hPackDecoder =
                new HPackDecoder(new DecodingContext(RequestedAuthority,
                    ArrayPoolMemoryProvider<char>.Default));

            _headerEncoder = new HeaderEncoder(hPackEncoder, hPackDecoder, _h2StreamSetting);
            _logger = new H2Logger(requestedAuthority, -1);
            _overallWindowSizeHolder = new WindowSizeHolder(_logger, _h2StreamSetting.OverallWindowSize, 0);
            _mainLoopTokenSource = new CancellationTokenSource();
            _mainLoopToken = _mainLoopTokenSource.Token;
        }

        public Authority RequestedAuthority { get; }

        public bool TunnelOnly { get; set; }

        public async Task Init(RsBuffer buffer)
        {
            // Read the client connection preface

            var prefaceMemory = buffer.Memory.Slice(0, H2Constants.Preface.Length);

            await _readStream.ReadExactAsync(prefaceMemory, _mainLoopToken);

            if (!prefaceMemory.Span.SequenceEqual(H2Constants.Preface)) {
                throw new FluxzyException("Invalid preface received");
            }

            // Send server connection preface (SETTINGS frame)
            await SendServerSettingsAsync();

            _readLoop = ReadLoop(_mainLoopToken);
            _writeLoop = WriteLoop(_mainLoopToken);
        }

        private async Task SendServerSettingsAsync()
        {
            var written = BuildServerSettingsFrame(out var settingBuffer);
            await _writeStream.WriteAsync(settingBuffer.AsMemory(0, written), _mainLoopToken);
            await _writeStream.FlushAsync(_mainLoopToken);
        }

        private int BuildServerSettingsFrame(out byte[] buffer)
        {
            buffer = new byte[512];
            var written = 0;

            var headerCount = 9;
            var totalSettingCount = 0;

            foreach (var (settingIdentifier, value) in _h2StreamSetting.GetAnnouncementSettings()) {
                written += SettingFrame.WriteMultipleBody(
                    buffer.AsSpan(written + headerCount), settingIdentifier, value);
                totalSettingCount++;
            }

            written += SettingFrame.WriteMultipleHeader(buffer.AsSpan(), totalSettingCount);

            var windowSizeAnnounced = _h2StreamSetting.Local.WindowSize - 65535;

            if (windowSizeAnnounced != 0) {
                var windowFrame = new WindowUpdateFrame(windowSizeAnnounced, 0);
                written += windowFrame.Write(buffer.AsSpan(written));
            }

            return written;
        }

        private void WriteSmallFrame(Span<byte> stackFrame, int length)
        {
            // Copy stackalloc'd frame to a pooled buffer for the channel
            var pooled = ArrayPool<byte>.Shared.Rent(length);
            stackFrame.Slice(0, length).CopyTo(pooled);
            _writeChannel.Writer.TryWrite(new PooledFrame(pooled, length, true));
        }

        private void WriteRstStream(int streamIdentifier, H2ErrorCode errorCode)
        {
            Span<byte> buffer = stackalloc byte[9 + 4];
            _ = new RstStreamFrame(streamIdentifier, errorCode).Write(buffer);
            WriteSmallFrame(buffer, 9 + 4);
        }

        private void WriteAck()
        {
            // SettingAckBuffer is a static readonly byte[] — not pooled, never returned
            _writeChannel.Writer.TryWrite(new PooledFrame(H2Helper.SettingAckBuffer, H2Helper.SettingAckBuffer.Length, false));
        }

        private async ValueTask NotifyConnectionWindowSizeDecrement(int length, CancellationToken token)
        {
            _unNotifiedWindowSize += length;

            if (_unNotifiedWindowSize > (_h2StreamSetting.Local.WindowSize / 2)) {

                SendWindowUpdateFrame(0, _unNotifiedWindowSize);
                _unNotifiedWindowSize = 0;
            }
        }

        private void SendWindowUpdateFrame(int streamIdentifier, int length)
        {
            Span<byte> buffer = stackalloc byte[9 + 4];
            var writtenLength = new WindowUpdateFrame(streamIdentifier, length).Write(buffer);
            WriteSmallFrame(buffer, writtenLength);
        }

        private void HandleWindowUpdate(ref H2FrameReadResult frame)
        {
            var windowSizeIncrement = frame.GetWindowUpdateFrame().WindowSizeIncrement;
            if (frame.StreamIdentifier == 0) {
                _overallWindowSizeHolder.UpdateWindowSize(windowSizeIncrement);
            }
            else if (_currentStreams.TryGetValue(frame.StreamIdentifier, out var streamWorker)) {
                streamWorker.UpdateWindowSize(windowSizeIncrement);
            }
        }

        private static void WritePingAck(ref H2FrameReadResult frame, Channel<PooledFrame> writeChannel)
        {
            var opaqueData = frame.GetPingFrame().OpaqueData;
            Span<byte> buffer = stackalloc byte[9 + 8];
            new PingFrame(opaqueData, HeaderFlags.Ack).Write(buffer);

            var pooled = ArrayPool<byte>.Shared.Rent(9 + 8);
            buffer.CopyTo(pooled);
            writeChannel.Writer.TryWrite(new PooledFrame(pooled, 9 + 8, true));
        }

        private void OnGoAwayReceived(int lastStreamId, H2ErrorCode errorCode)
        {
            _goAwayReceived = true;
            _goAwayErrorCode = errorCode;
        }

        private void CheckoutServerStreamWorker(ServerStreamWorker streamWorker)
        {
            _currentStreams.TryRemove(streamWorker.StreamIdentifier, out _);
            streamWorker.Dispose();
        }

        private async Task ReadLoop(CancellationToken token)
        {
            try {
                using var readBuffer = RsBuffer.Allocate(_h2StreamSetting.MaxFrameSizeAllowed + 9);

                while (!token.IsCancellationRequested) {

                    H2FrameReadResult frame;

                    try {
                        frame =
                            await H2FrameReader.ReadNextFrameAsync(_readStream, readBuffer.Memory,
                                token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) {
                        break;
                    }

                    if (frame.BodyType == H2FrameType.Settings) {
                        var sendAck = H2Helper.ProcessSettingFrame(_h2StreamSetting, frame);

                        if (sendAck)
                        {
                            WriteAck();
                        }

                        continue;
                    }

                    if (frame.BodyType == H2FrameType.Goaway) {
                        frame.GetGoAwayFrame().Read(out var errorCode, out _lastStreamId);
                        OnGoAwayReceived(_lastStreamId, errorCode);
                        break;
                    }

                    if (frame.BodyType == H2FrameType.Priority) {
                        // IGNORED
                        continue;
                    }

                    if (frame.BodyType == H2FrameType.WindowUpdate) {
                        HandleWindowUpdate(ref frame);
                        continue;
                    }

                    if (frame.BodyType == H2FrameType.RstStream) {
                        if (_currentStreams.TryGetValue(frame.StreamIdentifier, out var rstWorker)) {
                            CheckoutServerStreamWorker(rstWorker);
                        }
                        continue;
                    }

                    if (frame.BodyType == H2FrameType.Ping) {
                        WritePingAck(ref frame, _writeChannel);
                        continue;
                    }

                    //

                    if (!_currentStreams.TryGetValue(frame.StreamIdentifier, out var worker)) {
                        worker = new ServerStreamWorker(frame.StreamIdentifier, _headerEncoder,
                            _overallWindowSizeHolder, _h2StreamSetting, _logger);

                        _currentStreams.TryAdd(frame.StreamIdentifier, worker);
                    }

                    if (frame.BodyType == H2FrameType.PushPromise) {
                        var pushErrorCode = H2ErrorCode.ProtocolError;
                        WriteRstStream(frame.StreamIdentifier, pushErrorCode);
                        CheckoutServerStreamWorker(worker);
                    }

                    if (frame.BodyType == H2FrameType.Headers) {
                        var headerErrorCode = worker.ProcessHeaderFrame(ref frame);

                        if (headerErrorCode != H2ErrorCode.NoError)
                        {
                            WriteRstStream(frame.StreamIdentifier, headerErrorCode);
                            CheckoutServerStreamWorker(worker);
                        }
                    }

                    if (frame.BodyType == H2FrameType.Continuation) {
                        var contErrorCode = worker.ProcessContinuation(ref frame);

                        if (contErrorCode != H2ErrorCode.NoError)
                        {
                            WriteRstStream(frame.StreamIdentifier, contErrorCode);
                            CheckoutServerStreamWorker(worker);
                        }
                    }

                    if (frame.BodyType == H2FrameType.Data) {
                        var (dataErrorCode, bodyLength, notifiableLength) =
                            await worker.ReceiveBodyFragment(frame, readBuffer, token);

                        if (dataErrorCode != H2ErrorCode.NoError)
                        {
                            WriteRstStream(frame.StreamIdentifier, dataErrorCode);
                            CheckoutServerStreamWorker(worker);
                        }
                        else {
                            // send window size increment stream level
                            if (notifiableLength > 0) {
                                SendWindowUpdateFrame(frame.StreamIdentifier, notifiableLength.Value);
                            }

                            // send window size increment connection level
                            if (bodyLength > 0) {
                                await NotifyConnectionWindowSizeDecrement(bodyLength, token);
                            }
                        }
                    }

                    if (worker.ReadyToCreateExchange) {
                        var exchange = await worker.CreateExchange(_idProvider, _contextBuilder,
                            RequestedAuthority, true);

                        _exchangeChannel.Writer.TryWrite(exchange);
                    }
                }
            }
            catch (Exception ex) {
                throw;
            }
            finally  {
                _readHalted = true;
            }
        }

        private async Task WriteLoop(CancellationToken token)
        {
            try {

                while (!token.IsCancellationRequested && await _writeChannel.Reader.WaitToReadAsync(token)) {
                    while (_writeChannel.Reader.TryRead(out var frame)) {
                        await _writeStream.WriteAsync(frame.Memory, token);

                        if (frame.Pooled) {
                            ArrayPool<byte>.Shared.Return(frame.Array);
                        }
                    }
                    await _writeStream.FlushAsync(token);
                }
            }
            catch (Exception ex) {
                throw;
            }
            finally {
                // Return any remaining pooled frames
                while (_writeChannel.Reader.TryRead(out var remaining)) {
                    if (remaining.Pooled) {
                        ArrayPool<byte>.Shared.Return(remaining.Array);
                    }
                }

                _writeHalted = true;
            }
        }


        public async ValueTask<Exchange?> ReadNextExchange(RsBuffer buffer, ExchangeScope exchangeScope, CancellationToken token)
        {
            if (_disposed || _goAwayReceived || _readHalted || _writeHalted)
                return null;

            try {
                var exchange = await _exchangeChannel.Reader.ReadAsync(token);
                return exchange;
            }
            catch (ChannelClosedException) {
                return null;
            }
            catch (OperationCanceledException) when (_mainLoopToken.IsCancellationRequested) {
                return null;
            }
        }

        public async ValueTask WriteResponseHeader(
            ResponseHeader responseHeader, RsBuffer buffer, bool shouldClose, int streamIdentifier, ReadOnlyMemory<char> requestMethod, CancellationToken token)
        {
            var hasBody = responseHeader.HasResponseBody(requestMethod.Span, out _);

            byte[] pooledArray;
            int length;

            await _headerEncodeLock.WaitAsync(token);

            try {
                var payload = _headerEncoder.Encode(
                    new HeaderEncodingJob(responseHeader.GetHttp11Header(),
                        streamIdentifier, 0),
                    buffer, !hasBody);

                length = payload.Length;
                pooledArray = ArrayPool<byte>.Shared.Rent(length);
                payload.CopyTo(pooledArray);
            }
            finally {
                _headerEncodeLock.Release();
            }

            await _writeChannel.Writer.WriteAsync(new PooledFrame(pooledArray, length, true), token);
        }

        public async ValueTask WriteResponseBody(Stream responseBodyStream,
            RsBuffer rsBuffer, bool chunked, int streamIdentifier, Response? responseForTrailers, CancellationToken token)
        {
            // take care of window size
            if (!_currentStreams.TryGetValue(streamIdentifier, out var worker)) {

                // stream already closed
                throw new FluxzyException($"Invalid Local H2 stream : identifier {streamIdentifier}");
            }

            var sendStreamIdentifier = streamIdentifier;

            var remoteMaxFrameSize = _h2StreamSetting.Remote.MaxFrameSize;
            var readBuffer = ArrayPool<byte>.Shared.Rent(remoteMaxFrameSize + 9);

            try {
                int read;

                while ((read = await responseBodyStream
                           .ReadAsync(readBuffer.AsMemory().Slice(0, remoteMaxFrameSize), token)) > 0)
                {
                    int offset = 0;

                    while (read > 0)
                    {
                        var toWrite = await worker.BookWindowSize(read, token);

                        if (toWrite == 0)
                        {
                            // stream closed
                            return;
                        }

                        var bodySize = Math.Min(toWrite, remoteMaxFrameSize);

                        // Rent from pool — WriteLoop returns it after writing
                        var frameBuffer = ArrayPool<byte>.Shared.Rent(bodySize + 9);
                        new DataFrame(HeaderFlags.None, bodySize, sendStreamIdentifier)
                            .WriteHeaderOnly(frameBuffer, bodySize);
                        readBuffer.AsSpan(offset, bodySize).CopyTo(frameBuffer.AsSpan(9));
                        await _writeChannel.Writer.WriteAsync(
                            new PooledFrame(frameBuffer, bodySize + 9, true), token);

                        offset += bodySize;
                        read -= bodySize;
                    }
                }

                // Read trailers lazily — they are set by StreamWorker after the body pipe completes
                var trailers = responseForTrailers?.Trailers;

                if (trailers != null && trailers.Count > 0) {
                    // Send trailers as HEADERS frame with EndStream
                    await WriteResponseTrailers(trailers, rsBuffer, sendStreamIdentifier, token);
                }
                else {
                    // Send end stream — 9 bytes, rent from pool
                    var endFrameBuffer = ArrayPool<byte>.Shared.Rent(9);
                    new DataFrame(HeaderFlags.EndStream, 0, sendStreamIdentifier)
                        .WriteHeaderOnly(endFrameBuffer, 0);

                    await _writeChannel.Writer.WriteAsync(
                        new PooledFrame(endFrameBuffer, 9, true), token);
                }
            }
            finally {
                ArrayPool<byte>.Shared.Return(readBuffer);
            }
        }

        private async ValueTask WriteResponseTrailers(
            List<HeaderField> trailers, RsBuffer buffer, int streamIdentifier, CancellationToken token)
        {
            byte[] pooledArray;
            int length;

            await _headerEncodeLock.WaitAsync(token);

            try {
                var payload = _headerEncoder.EncodeTrailers(trailers, buffer, streamIdentifier);

                length = payload.Length;
                pooledArray = ArrayPool<byte>.Shared.Rent(length);
                payload.CopyTo(pooledArray);
            }
            finally {
                _headerEncodeLock.Release();
            }

            await _writeChannel.Writer.WriteAsync(new PooledFrame(pooledArray, length, true), token);
        }

        public (Stream ReadStream, Stream WriteStream) AbandonPipe()
        {
            return (_readStream, _writeStream);
        }

        public bool CanWrite => !_writeHalted;

        public bool SupportsMultiplexing => true;

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            _mainLoopTokenSource.Cancel();
            _writeChannel.Writer.TryComplete();
            _exchangeChannel.Writer.TryComplete();

            foreach (var (_, worker) in _currentStreams)
            {
                worker.Dispose();
            }

            _overallWindowSizeHolder.Dispose();
            _headerEncodeLock.Dispose();
        }
    }
}
