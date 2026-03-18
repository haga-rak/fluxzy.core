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
    internal class H2DownStreamPipe : IDownStreamPipe
    {
        private readonly Stream _readStream;
        private readonly Stream _writeStream;
        private readonly IIdProvider _idProvider;
        private readonly IExchangeContextBuilder _contextBuilder;

        private const int RingBufferCapacity = 512 * 1024;

        private readonly Channel<Exchange> _exchangeChannel =
            Channel.CreateUnbounded<Exchange>();

        private readonly CircularWriteBuffer _ringBuffer = new(RingBufferCapacity);

        private readonly ConcurrentDictionary<int, ServerStreamWorker> _currentStreams = new();
        private readonly HeaderEncoder _headerEncoder;
        private readonly SemaphoreSlim _headerEncodeLock = new SemaphoreSlim(1, 1);
        private readonly H2StreamSetting _h2StreamSetting = new H2StreamSetting();
        private readonly WindowSizeHolder _overallWindowSizeHolder;
        private readonly H2Logger _logger;
        private readonly CancellationToken _mainLoopToken;
        private readonly CancellationTokenSource _mainLoopTokenSource;

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

            await _readStream.ReadExactAsync(prefaceMemory, _mainLoopToken).ConfigureAwait(false);

            if (!prefaceMemory.Span.SequenceEqual(H2Constants.Preface)) {
                throw new FluxzyException("Invalid preface received");
            }

            // Send server connection preface (SETTINGS frame)
            await SendServerSettingsAsync().ConfigureAwait(false);

            ReadLoop(_mainLoopToken);
            WriteLoop(_mainLoopToken);
        }

        private async Task SendServerSettingsAsync()
        {
            var written = BuildServerSettingsFrame(out var settingBuffer);
            await _writeStream.WriteAsync(settingBuffer.AsMemory(0, written), _mainLoopToken).ConfigureAwait(false);
            await _writeStream.FlushAsync(_mainLoopToken).ConfigureAwait(false);
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
            _ringBuffer.Write(stackFrame.Slice(0, length));
        }

        private void WriteRstStream(int streamIdentifier, H2ErrorCode errorCode)
        {
            Span<byte> buffer = stackalloc byte[9 + 4];
            _ = new RstStreamFrame(streamIdentifier, errorCode).Write(buffer);
            WriteSmallFrame(buffer, 9 + 4);
        }

        private void WriteAck()
        {
            _ringBuffer.Write(H2Helper.SettingAckBuffer);
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

        private void WritePingAck(ref H2FrameReadResult frame)
        {
            var opaqueData = frame.GetPingFrame().OpaqueData;
            Span<byte> buffer = stackalloc byte[9 + 8];
            new PingFrame(opaqueData, HeaderFlags.Ack).Write(buffer);
            _ringBuffer.Write(buffer);
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

                    if (frame.IsEmpty) {
                        // EOF — peer closed the connection
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
                        WritePingAck(ref frame);
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
                            await worker.ReceiveBodyFragment(frame, readBuffer, token).ConfigureAwait(false);

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
                                await NotifyConnectionWindowSizeDecrement(bodyLength, token).ConfigureAwait(false);
                            }
                        }
                    }

                    if (worker.ReadyToCreateExchange) {
                        var exchange = await worker.CreateExchange(_idProvider, _contextBuilder,
                            RequestedAuthority, true).ConfigureAwait(false);

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
                while (!token.IsCancellationRequested &&
                       await _ringBuffer.WaitForDataAsync(token).ConfigureAwait(false)) {

                    _ringBuffer.GetReadableRegions(out var seg1, out var seg2, out var total);

                    if (seg1.Length > 0)
                        await _writeStream.WriteAsync(seg1, token).ConfigureAwait(false);

                    if (seg2.Length > 0)
                        await _writeStream.WriteAsync(seg2, token).ConfigureAwait(false);

                    _ringBuffer.Advance(total);

                    await _writeStream.FlushAsync(token).ConfigureAwait(false);
                }
            }
            catch (Exception ex) {
                throw;
            }
            finally {
                _writeHalted = true;
            }
        }


        public async ValueTask<Exchange?> ReadNextExchange(RsBuffer buffer, ExchangeScope exchangeScope, CancellationToken token)
        {
            if (_disposed || _goAwayReceived || _readHalted || _writeHalted)
                return null;

            try {
                var exchange = await _exchangeChannel.Reader.ReadAsync(token).ConfigureAwait(false);
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

            await _headerEncodeLock.WaitAsync(token).ConfigureAwait(false);

            try {
                var payload = _headerEncoder.Encode(
                    new HeaderEncodingJob(responseHeader.GetHttp11Header(),
                        streamIdentifier, 0),
                    buffer, !hasBody);

                _ringBuffer.Write(payload.Span);
            }
            finally {
                _headerEncodeLock.Release();
            }
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

            // Single rent reused across all iterations — returned once at the end
            var frameBuffer = ArrayPool<byte>.Shared.Rent(remoteMaxFrameSize + 9);

            try {
                while (true)
                {
                    var booked = await worker.BookWindowSize(remoteMaxFrameSize, token).ConfigureAwait(false);

                    if (booked == 0)
                    {
                        // stream closed
                        return;
                    }

                    var bodySize = Math.Min(booked, remoteMaxFrameSize);

                    var read = await responseBodyStream
                        .ReadAsync(frameBuffer.AsMemory(9, bodySize), token).ConfigureAwait(false);

                    if (read == 0)
                    {
                        // EOF — refund booked window and break to send end-stream
                        worker.RefundWindowSize(booked);
                        break;
                    }

                    // Refund unused window if we read less than booked
                    var refund = booked - read;

                    if (refund > 0) {
                        worker.RefundWindowSize(refund);
                    }

                    new DataFrame(HeaderFlags.None, read, sendStreamIdentifier)
                        .WriteHeaderOnly(frameBuffer, read);

                    _ringBuffer.Write(new ReadOnlySpan<byte>(frameBuffer, 0, read + 9));
                }
            }
            finally {
                ArrayPool<byte>.Shared.Return(frameBuffer);
            }

            // Return unused budget to holders so other streams can use the window.
            worker.DrainBudget();

            // Read trailers lazily — they are set by StreamWorker after the body pipe completes
            var trailers = responseForTrailers?.Trailers;

            if (trailers != null && trailers.Count > 0) {
                // Send trailers as HEADERS frame with EndStream
                await WriteResponseTrailers(trailers, rsBuffer, sendStreamIdentifier, token).ConfigureAwait(false);
            }
            else {
                WriteEndStream(sendStreamIdentifier);
            }
        }

        private void WriteEndStream(int streamIdentifier)
        {
            Span<byte> endFrame = stackalloc byte[9];
            new DataFrame(HeaderFlags.EndStream, 0, streamIdentifier)
                .WriteHeaderOnly(endFrame, 0);
            _ringBuffer.Write(endFrame);
        }

        private async ValueTask WriteResponseTrailers(
            List<HeaderField> trailers, RsBuffer buffer, int streamIdentifier, CancellationToken token)
        {
            await _headerEncodeLock.WaitAsync(token).ConfigureAwait(false);

            try {
                var payload = _headerEncoder.EncodeTrailers(trailers, buffer, streamIdentifier);
                _ringBuffer.Write(payload.Span);
            }
            finally {
                _headerEncodeLock.Release();
            }
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
            _ringBuffer.Complete();
            _exchangeChannel.Writer.TryComplete();

            foreach (var (_, worker) in _currentStreams)
            {
                worker.Dispose();
            }

            _overallWindowSizeHolder.Dispose();
            _headerEncodeLock.Dispose();
            _ringBuffer.Dispose();
        }
    }
}
