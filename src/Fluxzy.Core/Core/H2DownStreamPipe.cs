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
    internal readonly struct DataFrameEntry
    {
        public readonly byte[] RentedBuffer;
        public readonly int Length;
        public readonly int FlowControlledBytes;

        public DataFrameEntry(byte[] rentedBuffer, int length, int flowControlledBytes)
        {
            RentedBuffer = rentedBuffer;
            Length = length;
            FlowControlledBytes = flowControlledBytes;
        }
    }

    internal class H2DownStreamPipe : IDownStreamPipe
    {
        private readonly Stream _readStream;
        private readonly Stream _writeStream;
        private readonly IIdProvider _idProvider;
        private readonly IExchangeContextBuilder _contextBuilder;

        private const int RingBufferCapacity = 512 * 1024;

        private readonly Channel<Exchange> _exchangeChannel =
            Channel.CreateUnbounded<Exchange>();

        private readonly CircularWriteBuffer _ringBuffer;

        private readonly ConcurrentDictionary<int, ServerStreamWorker> _currentStreams = new();
        private readonly HeaderEncoder _headerEncoder;
        private readonly object _headerEncodeLock = new();
        private readonly H2StreamSetting _h2StreamSetting = new H2StreamSetting() {
            Local = new () {
                SettingsMaxConcurrentStreams = 256
            }
        };

        private readonly Channel<DataFrameEntry> _dataChannel;
        private const int GatherBufferSize = 256 * 1024;
        private int _connectionWindow = 65535;
        private readonly SemaphoreSlim _writeSignal = new(0);
        private int _writeSignalState;

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
        private int _highestAcceptedStreamId;
        private bool _goAwaySent;

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
            _ringBuffer = new CircularWriteBuffer(RingBufferCapacity, SignalWriteLoop);
            _dataChannel = Channel.CreateUnbounded<DataFrameEntry>(
                new UnboundedChannelOptions() { SingleReader = true });
            _mainLoopTokenSource = new CancellationTokenSource();
            _mainLoopToken = _mainLoopTokenSource.Token;
        }

        public Authority RequestedAuthority { get; }

        public bool TunnelOnly { get; set; }

        private void SignalWriteLoop()
        {
            if (Interlocked.CompareExchange(ref _writeSignalState, 1, 0) == 0)
                _writeSignal.Release();
        }

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

            _ = ReadLoop(_mainLoopToken);
            _ = WriteLoop(_mainLoopToken);
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

        private void WriteGoAway(H2ErrorCode errorCode)
        {
            if (_goAwaySent)
                return;

            _goAwaySent = true;

            Span<byte> buffer = stackalloc byte[9 + 8];
            new GoAwayFrame(_highestAcceptedStreamId, errorCode).Write(buffer);
            WriteSmallFrame(buffer, 9 + 8);
        }

        private void WriteAck()
        {
            _ringBuffer.Write(H2Helper.SettingAckBuffer);
        }

        private void NotifyConnectionWindowSizeDecrement(int length, CancellationToken token)
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
            var writtenLength = new WindowUpdateFrame(length, streamIdentifier).Write(buffer);
            WriteSmallFrame(buffer, writtenLength);
        }

        private void HandleWindowUpdate(ref H2FrameReadResult frame)
        {
            var windowSizeIncrement = frame.GetWindowUpdateFrame().WindowSizeIncrement;
            if (frame.StreamIdentifier == 0) {
                Interlocked.Add(ref _connectionWindow, windowSizeIncrement);
                SignalWriteLoop();
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

            if (errorCode != H2ErrorCode.NoError && DebugContext.EnableDumpStackTraceOn502)
                Console.Error.WriteLine($"H2 downstream GO_AWAY received ({RequestedAuthority}): errorCode={errorCode}, lastStreamId={lastStreamId}");

            foreach (var (streamId, worker) in _currentStreams) {
                if (streamId > lastStreamId) {
                    if (_currentStreams.TryRemove(streamId, out _))
                        worker.Dispose();
                }
            }
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
                        var sendAck = H2Helper.ProcessSettingFrame(_h2StreamSetting, frame, out var fatalError);

                        if (fatalError.HasValue) {
                            WriteGoAway(fatalError.Value);
                            break;
                        }

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
                            _h2StreamSetting, _logger);

                        _currentStreams.TryAdd(frame.StreamIdentifier, worker);

                        if (frame.StreamIdentifier > _highestAcceptedStreamId)
                            _highestAcceptedStreamId = frame.StreamIdentifier;
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
                                NotifyConnectionWindowSizeDecrement(bodyLength, token);
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
                if (DebugContext.EnableDumpStackTraceOn502)
                    Console.Error.WriteLine($"H2 downstream read loop error ({RequestedAuthority}): {ex}");

                try { WriteGoAway(H2ErrorCode.InternalError); }
                catch { /* best-effort */ }

                throw;
            }
            finally  {
                _readHalted = true;
            }
        }

        private async Task WriteLoop(CancellationToken token)
        {
            try {
                while (!token.IsCancellationRequested) {
                    var didWork = false;

                    // Phase 1: Drain ring buffer (control frames, HEADERS — priority, no flow control)
                    _ringBuffer.GetReadableRegions(out var seg1, out var seg2, out var total);

                    if (total > 0) {
                        if (seg1.Length > 0)
                            await _writeStream.WriteAsync(seg1, token).ConfigureAwait(false);

                        if (seg2.Length > 0)
                            await _writeStream.WriteAsync(seg2, token).ConfigureAwait(false);

                        _ringBuffer.Advance(total);
                        didWork = true;
                    }

                    // Phase 2: Drain data channel respecting connection window.
                    // Gather consecutive DATA frames into a single write to reduce syscalls.
                    byte[]? gatherBuffer = null;
                    var gatherOffset = 0;

                    while (_dataChannel.Reader.TryPeek(out var entry)) {
                        // Interleave: flush gathered data and drain ring buffer if priority data exists
                        if (_ringBuffer.ReadableCount > 0) {
                            if (gatherOffset > 0) {
                                await _writeStream.WriteAsync(gatherBuffer!.AsMemory(0, gatherOffset), token).ConfigureAwait(false);
                                gatherOffset = 0;
                                didWork = true;
                            }

                            _ringBuffer.GetReadableRegions(out var pri1, out var pri2, out var priTotal);

                            if (priTotal > 0) {
                                if (pri1.Length > 0)
                                    await _writeStream.WriteAsync(pri1, token).ConfigureAwait(false);

                                if (pri2.Length > 0)
                                    await _writeStream.WriteAsync(pri2, token).ConfigureAwait(false);

                                _ringBuffer.Advance(priTotal);
                                didWork = true;
                            }
                        }

                        if (entry.FlowControlledBytes > 0) {
                            var window = Volatile.Read(ref _connectionWindow);

                            if (window < entry.FlowControlledBytes)
                                break; // connection window exhausted

                            Interlocked.Add(ref _connectionWindow, -entry.FlowControlledBytes);
                        }

                        _dataChannel.Reader.TryRead(out _); // consume the peeked entry

                        // Single frame with nothing else queued — write directly, skip gather
                        if (gatherOffset == 0 && !_dataChannel.Reader.TryPeek(out _)) {
                            await _writeStream.WriteAsync(
                                entry.RentedBuffer.AsMemory(0, entry.Length), token).ConfigureAwait(false);
                            ArrayPool<byte>.Shared.Return(entry.RentedBuffer);
                            didWork = true;
                            break;
                        }

                        // Gather mode: accumulate frames for batched write
                        gatherBuffer ??= ArrayPool<byte>.Shared.Rent(GatherBufferSize);

                        if (gatherOffset + entry.Length > gatherBuffer.Length) {
                            // Flush current batch before it overflows
                            if (gatherOffset > 0) {
                                await _writeStream.WriteAsync(gatherBuffer.AsMemory(0, gatherOffset), token).ConfigureAwait(false);
                                gatherOffset = 0;
                                didWork = true;
                            }
                        }

                        entry.RentedBuffer.AsSpan(0, entry.Length).CopyTo(gatherBuffer.AsSpan(gatherOffset));
                        gatherOffset += entry.Length;
                        ArrayPool<byte>.Shared.Return(entry.RentedBuffer);
                        didWork = true;
                    }

                    // Flush remaining gathered data
                    if (gatherOffset > 0) {
                        await _writeStream.WriteAsync(gatherBuffer!.AsMemory(0, gatherOffset), token).ConfigureAwait(false);
                        didWork = true;
                    }

                    if (gatherBuffer != null)
                        ArrayPool<byte>.Shared.Return(gatherBuffer);

                    // Phase 3: Flush
                    if (didWork)
                        await _writeStream.FlushAsync(token).ConfigureAwait(false);

                    // Phase 4: Wait for signal
                    Interlocked.Exchange(ref _writeSignalState, 0);

                    // Double-check all sources before sleeping
                    if (_ringBuffer.ReadableCount > 0)
                        continue;

                    if (_dataChannel.Reader.TryPeek(out _))
                        continue;

                    // Check termination: both sources completed and empty
                    if (_ringBuffer.IsCompleted && _dataChannel.Reader.Completion.IsCompleted) {
                        // Final drain to catch any data that arrived between checks
                        if (_ringBuffer.ReadableCount > 0 || _dataChannel.Reader.TryPeek(out _))
                            continue;

                        break;
                    }
                    
                    await _writeSignal.WaitAsync(token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
            catch (Exception ex) {
                if (DebugContext.EnableDumpStackTraceOn502)
                    Console.Error.WriteLine($"H2 downstream write loop error ({RequestedAuthority}): {ex}");
                throw;
            }
            finally {
                // Return rented buffers from any remaining channel entries
                while (_dataChannel.Reader.TryRead(out var remaining))
                    ArrayPool<byte>.Shared.Return(remaining.RentedBuffer);

                _writeHalted = true;
            }
        }


        public async ValueTask<Exchange?> ReadNextExchange(RsBuffer buffer, ExchangeScope exchangeScope, CancellationToken token)
        {
            if (_disposed || _goAwayReceived || _goAwaySent || _readHalted || _writeHalted)
                return null;

            try {
                if (_exchangeChannel.Reader.TryRead(out var exchange))
                    return exchange;

                exchange = await _exchangeChannel.Reader.ReadAsync(token).ConfigureAwait(false);
                return exchange;
            }
            catch (ChannelClosedException) {
                return null;
            }
            catch (OperationCanceledException) when (_mainLoopToken.IsCancellationRequested) {
                return null;
            }
        }

        public ValueTask WriteResponseHeader(
            ResponseHeader responseHeader, RsBuffer buffer, bool shouldClose, int streamIdentifier, ReadOnlyMemory<char> requestMethod, CancellationToken token)
        {
            var hasBody = responseHeader.HasResponseBody(requestMethod.Span, out _);
            ReadOnlyMemory<byte> payload;

            lock (_headerEncodeLock) {
                payload = _headerEncoder.Encode(
                    new HeaderEncodingJob(responseHeader.GetHttp11Header(),
                        streamIdentifier, 0),
                    buffer, !hasBody);
            }

            _ringBuffer.Write(payload.Span);

            if (!hasBody) {
                // No body will follow — clean up the stream worker now.
                if (_currentStreams.TryRemove(streamIdentifier, out var worker)) {
                    worker.Dispose();
                }
            }

            return default;
        }

        public async ValueTask WriteResponseBody(Stream responseBodyStream,
            RsBuffer rsBuffer, bool chunked, int streamIdentifier, Response? responseForTrailers, CancellationToken token)
        {
            // take care of window size
            if (!_currentStreams.TryGetValue(streamIdentifier, out var worker)) {

                // stream already closed
                throw new FluxzyException($"Invalid Local H2 stream : identifier {streamIdentifier}");
            }

            var remoteMaxFrameSize = _h2StreamSetting.Remote.MaxFrameSize;

            while (true)
            {
                var booked = await worker.BookWindowSize(remoteMaxFrameSize, token).ConfigureAwait(false);

                if (booked == 0)
                {
                    // stream closed
                    return;
                }

                var bodySize = Math.Min(booked, remoteMaxFrameSize);

                // Rent buffer upfront; read body directly at offset 9 (after frame header)
                var rentedBuffer = ArrayPool<byte>.Shared.Rent(bodySize + 9);

                var read = await responseBodyStream
                    .ReadAsync(rentedBuffer.AsMemory(9, bodySize), token).ConfigureAwait(false);

                if (read == 0)
                {
                    // EOF — return buffer, refund booked window, break to send end-stream
                    ArrayPool<byte>.Shared.Return(rentedBuffer);
                    worker.RefundWindowSize(booked);
                    break;
                }

                // Refund unused window if we read less than booked
                var refund = booked - read;

                if (refund > 0) {
                    worker.RefundWindowSize(refund);
                }

                // Build DATA frame header directly in rented buffer
                new DataFrame(HeaderFlags.None, read, streamIdentifier)
                    .WriteHeaderOnly(rentedBuffer, read);

                var frameLength = read + 9;

                await _dataChannel.Writer.WriteAsync(
                    new DataFrameEntry(rentedBuffer, frameLength, read), token).ConfigureAwait(false);

                SignalWriteLoop();
            }

            // Read trailers lazily — they are set by StreamWorker after the body pipe completes
            var trailers = responseForTrailers?.Trailers;

            if (trailers != null && trailers.Count > 0) {
                // Send trailers as HEADERS frame with EndStream, enqueue to data channel
                ReadOnlyMemory<byte> payload;

                lock (_headerEncodeLock) {
                    payload = _headerEncoder.EncodeTrailers(trailers, rsBuffer, streamIdentifier);
                }

                var rentedTrailerBuffer = ArrayPool<byte>.Shared.Rent(payload.Length);
                payload.Span.CopyTo(rentedTrailerBuffer);

                await _dataChannel.Writer.WriteAsync(
                    new DataFrameEntry(rentedTrailerBuffer, payload.Length, 0), token).ConfigureAwait(false);

                SignalWriteLoop();
            }
            else {
                // Send 0-byte EndStream DATA frame
                var endFrameBuffer = ArrayPool<byte>.Shared.Rent(9);

                new DataFrame(HeaderFlags.EndStream, 0, streamIdentifier)
                    .WriteHeaderOnly(endFrameBuffer, 0);

                await _dataChannel.Writer.WriteAsync(
                    new DataFrameEntry(endFrameBuffer, 9, 0), token).ConfigureAwait(false);

                SignalWriteLoop();
            }

            // Stream is fully complete — clean up the worker.
            if (_currentStreams.TryRemove(streamIdentifier, out var completedWorker)) {
                completedWorker.Dispose();
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

            if (!_goAwaySent && !_writeHalted) {
                try { WriteGoAway(H2ErrorCode.NoError); }
                catch { /* best-effort */ }
            }

            _mainLoopTokenSource.Cancel();
            _ringBuffer.Complete();
            _dataChannel.Writer.TryComplete();
            _exchangeChannel.Writer.TryComplete();

            foreach (var (_, worker) in _currentStreams)
            {
                worker.Dispose();
            }

            _ringBuffer.Dispose();
        }
    }
}
