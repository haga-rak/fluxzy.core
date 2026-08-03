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
            Channel.CreateUnbounded<Exchange>(new () {
                SingleWriter = true, SingleReader = true
            });

        private readonly CircularWriteBuffer _ringBuffer;

        private readonly ConcurrentDictionary<int, ServerStreamWorker> _currentStreams = new();
        private readonly HeaderEncoder _headerEncoder;
        private readonly Channel<PendingHeaderWrite> _pendingHeaders =
            Channel.CreateUnbounded<PendingHeaderWrite>(
                new UnboundedChannelOptions() { SingleReader = true });
        private readonly RsBuffer _headerEncodeBuffer = RsBuffer.Allocate(16 * 1024);
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
        private int _writeLoopIterations;
        private int _closedStreamCount;
        private int _droppedResponseBufferCount;
        private int _responseDataEnqueuedCount;
        private int _responseDataEnqueuedWaitTarget;
        private TaskCompletionSource<object?>? _responseDataEnqueuedWaiter;
        private int _activeStreamWaitIdentifier;
        private TaskCompletionSource<object?>? _activeStreamWaiter;
        private TaskCompletionSource<object?>? _writeLoopGateForTests;
        private TaskCompletionSource<object?>? _writeLoopIdleForTests;

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
        private int _expectedContinuationStreamId;

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
                    worker.Abort(errorCode);
                    CheckoutServerStreamWorker(worker);
                }
            }
        }

        private void CheckoutServerStreamWorker(ServerStreamWorker streamWorker)
        {
            if (_currentStreams.TryRemove(streamWorker.StreamIdentifier, out var removedWorker)) {
                removedWorker.Dispose();
                Interlocked.Increment(ref _closedStreamCount);
            }
        }

        private void AbortServerStreamWorker(ServerStreamWorker streamWorker, H2ErrorCode errorCode)
        {
            streamWorker.Abort(errorCode);
            CheckoutServerStreamWorker(streamWorker);
        }

        private async Task ReadLoop(CancellationToken token)
        {
            try {
                using var reader = new H2FrameStreamReader(_readStream, _h2StreamSetting.MaxFrameSizeAllowed);
                using var readBuffer = RsBuffer.Allocate(_h2StreamSetting.MaxFrameSizeAllowed + 9);

                while (!token.IsCancellationRequested) {

                    H2FrameReadResult frame;

                    try {
                        frame = await reader.ReadNextFrameAsync(token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) {
                        break;
                    }

                    if (frame.IsEmpty) {
                        // EOF — peer closed the connection
                        break;
                    }

                    if (_expectedContinuationStreamId != 0 &&
                        (frame.BodyType != H2FrameType.Continuation ||
                         frame.StreamIdentifier != _expectedContinuationStreamId)) {
                        if (_currentStreams.TryGetValue(
                                _expectedContinuationStreamId, out var fragmentedWorker))
                            AbortServerStreamWorker(fragmentedWorker, H2ErrorCode.ProtocolError);

                        WriteGoAway(H2ErrorCode.ProtocolError);
                        break;
                    }

                    if (_expectedContinuationStreamId == 0 &&
                        frame.BodyType == H2FrameType.Continuation) {
                        WriteGoAway(H2ErrorCode.ProtocolError);
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
                            AbortServerStreamWorker(
                                rstWorker, frame.GetRstStreamFrame().ErrorCode);
                        }
                        continue;
                    }

                    if (frame.BodyType == H2FrameType.Ping) {
                        WritePingAck(ref frame);
                        continue;
                    }

                    if (!_currentStreams.TryGetValue(frame.StreamIdentifier, out var worker)) {
                        if (frame.BodyType != H2FrameType.Headers) {
                            if (frame.StreamIdentifier <= _highestAcceptedStreamId)
                                continue;

                            WriteGoAway(H2ErrorCode.ProtocolError);
                            break;
                        }

                        if (frame.StreamIdentifier <= 0 || (frame.StreamIdentifier & 1) == 0 ||
                            frame.StreamIdentifier <= _highestAcceptedStreamId) {
                            WriteGoAway(H2ErrorCode.ProtocolError);
                            break;
                        }

                        worker = new ServerStreamWorker(frame.StreamIdentifier, _headerEncoder,
                            _h2StreamSetting);

                        _currentStreams.TryAdd(frame.StreamIdentifier, worker);
                        _highestAcceptedStreamId = frame.StreamIdentifier;

                        if (frame.StreamIdentifier == Volatile.Read(ref _activeStreamWaitIdentifier))
                            Interlocked.Exchange(ref _activeStreamWaiter, null)?.TrySetResult(null);
                    }

                    if (frame.BodyType == H2FrameType.PushPromise) {
                        var pushErrorCode = H2ErrorCode.ProtocolError;
                        WriteRstStream(frame.StreamIdentifier, pushErrorCode);
                        AbortServerStreamWorker(worker, pushErrorCode);
                        continue;
                    }

                    if (frame.BodyType == H2FrameType.Headers) {
                        var headerErrorCode = worker.ProcessHeaderFrame(ref frame);

                        if (headerErrorCode != H2ErrorCode.NoError)
                        {
                            WriteRstStream(frame.StreamIdentifier, headerErrorCode);
                            AbortServerStreamWorker(worker, headerErrorCode);
                            continue;
                        }

                        if (!frame.GetHeadersFrame().EndHeaders)
                            _expectedContinuationStreamId = frame.StreamIdentifier;
                    }

                    if (frame.BodyType == H2FrameType.Continuation) {
                        var contErrorCode = worker.ProcessContinuation(ref frame);

                        if (contErrorCode != H2ErrorCode.NoError)
                        {
                            if (frame.GetContinuationFrame().EndHeaders)
                                _expectedContinuationStreamId = 0;

                            WriteRstStream(frame.StreamIdentifier, contErrorCode);
                            AbortServerStreamWorker(worker, contErrorCode);
                            continue;
                        }

                        if (frame.GetContinuationFrame().EndHeaders)
                            _expectedContinuationStreamId = 0;
                    }

                    if (frame.BodyType == H2FrameType.Data) {
                        var (dataErrorCode, bodyLength, notifiableLength) =
                            await worker.ReceiveBodyFragment(frame, readBuffer, token).ConfigureAwait(false);

                        if (dataErrorCode != H2ErrorCode.NoError)
                        {
                            WriteRstStream(frame.StreamIdentifier, dataErrorCode);
                            AbortServerStreamWorker(worker, dataErrorCode);
                            continue;
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

                    if (worker.IsClosed) {
                        CheckoutServerStreamWorker(worker);
                        continue;
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

        private async ValueTask FlushRingBufferAsync(CancellationToken token)
        {
            _ringBuffer.GetReadableRegions(out var seg1, out var seg2, out var total);

            if (total > 0) {
                if (seg1.Length > 0)
                    await _writeStream.WriteAsync(seg1, token).ConfigureAwait(false);

                if (seg2.Length > 0)
                    await _writeStream.WriteAsync(seg2, token).ConfigureAwait(false);

                _ringBuffer.Advance(total);
            }
        }

        /// <summary>
        ///     Test seam: outer-while iteration counter for <see cref="WriteLoop"/>. Used
        ///     to detect spin-loop regressions where the loop re-enters Phase 2 faster
        ///     than WINDOW_UPDATEs can arrive (see WriteLoop_DoesNotSpinWhenConnectionWindowExhausted).
        /// </summary>
        internal int WriteLoopIterationsForTests => Volatile.Read(ref _writeLoopIterations);
        internal int ActiveStreamCountForTests => _currentStreams.Count;
        internal int ClosedStreamCountForTests => Volatile.Read(ref _closedStreamCount);
        internal int DroppedResponseBufferCountForTests
            => Volatile.Read(ref _droppedResponseBufferCount);

        internal void PauseWriteLoopForTests()
        {
            var gate = new TaskCompletionSource<object?>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            if (Interlocked.CompareExchange(ref _writeLoopGateForTests, gate, null) != null)
                throw new InvalidOperationException("The HTTP/2 write loop is already paused");

            SignalWriteLoop();
        }

        internal void ResumeWriteLoopForTests()
            => Interlocked.Exchange(ref _writeLoopGateForTests, null)?.TrySetResult(null);

        internal Task WaitForWriteLoopIdleForTests()
        {
            var waiter = new TaskCompletionSource<object?>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            if (Interlocked.CompareExchange(ref _writeLoopIdleForTests, waiter, null) != null)
                throw new InvalidOperationException("A write-loop idle waiter is already registered");

            SignalWriteLoop();
            return waiter.Task;
        }

        internal Task WaitForResponseDataEntriesForTests(int count)
        {
            if (Volatile.Read(ref _responseDataEnqueuedCount) >= count)
                return Task.CompletedTask;

            var waiter = new TaskCompletionSource<object?>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            Volatile.Write(ref _responseDataEnqueuedWaitTarget, count);

            if (Interlocked.CompareExchange(
                    ref _responseDataEnqueuedWaiter, waiter, null) != null)
                throw new InvalidOperationException("A response-data waiter is already registered");

            if (Volatile.Read(ref _responseDataEnqueuedCount) >= count)
                Interlocked.Exchange(ref _responseDataEnqueuedWaiter, null)?.TrySetResult(null);

            return waiter.Task;
        }

        internal Task WaitForStreamActiveForTests(int streamIdentifier)
        {
            if (_currentStreams.ContainsKey(streamIdentifier))
                return Task.CompletedTask;

            var waiter = new TaskCompletionSource<object?>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            Volatile.Write(ref _activeStreamWaitIdentifier, streamIdentifier);

            if (Interlocked.CompareExchange(ref _activeStreamWaiter, waiter, null) != null)
                throw new InvalidOperationException("An active-stream waiter is already registered");

            if (_currentStreams.ContainsKey(streamIdentifier))
                Interlocked.Exchange(ref _activeStreamWaiter, null)?.TrySetResult(null);

            return waiter.Task;
        }

        private void NotifyResponseDataEnqueuedForTests()
        {
            var count = Interlocked.Increment(ref _responseDataEnqueuedCount);

            if (count >= Volatile.Read(ref _responseDataEnqueuedWaitTarget))
                Interlocked.Exchange(ref _responseDataEnqueuedWaiter, null)?.TrySetResult(null);
        }

        /// <summary>
        ///     Test seam: injects a synthetic <see cref="DataFrameEntry"/> with the given
        ///     flow-control cost directly into the data channel, bypassing the
        ///     ServerStreamWorker/response-body machinery. Lets tests force the
        ///     "connection window exhausted, entry queued" state deterministically.
        /// </summary>
        internal void EnqueueFlowControlledDataForTest(int flowControlledBytes)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(9);
            _dataChannel.Writer.TryWrite(new DataFrameEntry(buffer, 9, flowControlledBytes, 0));
            SignalWriteLoop();
        }

        private async Task WriteLoop(CancellationToken token)
        {
            var gatherBuffer = ArrayPool<byte>.Shared.Rent(GatherBufferSize);
            var completedResponses = new List<int>();

            try {
                while (!token.IsCancellationRequested) {
                    Interlocked.Increment(ref _writeLoopIterations);
                    var didWork = false;

                    if (Volatile.Read(ref _writeLoopGateForTests) is { } writeLoopGate)
                        await writeLoopGate.Task.WaitAsync(token).ConfigureAwait(false);

                    // Phase 1: Drain pending header encodes into the ring buffer, then drain
                    //          the ring buffer (control frames + HEADERS — priority, no flow control).
                    //          Encoding runs only here, so the shared HPACK dynamic table needs
                    //          no synchronization.
                    while (_pendingHeaders.Reader.TryRead(out var pending)) {
                        var bodylessWorker = EncodePendingHeader(pending);

                        if (bodylessWorker != null)
                            await CompleteBodylessHeaderAsync(bodylessWorker, token).ConfigureAwait(false);

                        didWork = true;
                    }

                    if (_ringBuffer.ReadableCount > 0) {
                        await FlushRingBufferAsync(token).ConfigureAwait(false);
                        didWork = true;
                    }

                    // Phase 2: Drain data channel respecting connection window.
                    // Gather consecutive DATA frames into a single write to reduce syscalls.
                    var gatherOffset = 0;

                    while (_dataChannel.Reader.TryPeek(out var entry)) {
                        // Re-drain any pending headers that arrived during data writes so the
                        // HEADERS frame for stream X always precedes its DATA on the wire.
                        // Headers go into the ring buffer, which the interleave block below flushes.
                        while (_pendingHeaders.Reader.TryRead(out var pending)) {
                            if (gatherOffset > 0) {
                                await FlushGatheredDataAsync(
                                    gatherBuffer, gatherOffset, completedResponses, token)
                                    .ConfigureAwait(false);
                                gatherOffset = 0;
                            }

                            var bodylessWorker = EncodePendingHeader(pending);

                            if (bodylessWorker != null)
                                await CompleteBodylessHeaderAsync(bodylessWorker, token).ConfigureAwait(false);

                            didWork = true;
                        }

                        // Interleave: flush gathered data and drain ring buffer if priority data exists
                        if (_ringBuffer.ReadableCount > 0) {
                            if (gatherOffset > 0) {
                                await FlushGatheredDataAsync(
                                    gatherBuffer, gatherOffset, completedResponses, token)
                                    .ConfigureAwait(false);
                                gatherOffset = 0;
                                didWork = true;
                            }

                            await FlushRingBufferAsync(token).ConfigureAwait(false);
                            didWork = true;
                        }

                        // Trailer-encoding job (placed inline in the DATA channel to preserve
                        // per-stream ordering relative to the DATA frames queued ahead of it).
                        if (entry.TrailerHeaders != null) {
                            if (gatherOffset > 0) {
                                await FlushGatheredDataAsync(
                                    gatherBuffer, gatherOffset, completedResponses, token)
                                    .ConfigureAwait(false);
                                gatherOffset = 0;
                            }

                            if (!_currentStreams.TryGetValue(entry.StreamIdentifier, out var trailerWorker) ||
                                trailerWorker.IsAborted) {
                                _dataChannel.Reader.TryRead(out _);
                                continue;
                            }

                            var trailerBytes = _headerEncoder.EncodeTrailers(
                                entry.TrailerHeaders, _headerEncodeBuffer, entry.StreamIdentifier);

                            await _writeStream.WriteAsync(trailerBytes, token).ConfigureAwait(false);
                            _dataChannel.Reader.TryRead(out _); // consume the peeked entry
                            CompleteWrittenResponse(entry.StreamIdentifier);
                            didWork = true;
                            continue;
                        }

                        if (entry.StreamIdentifier != 0 &&
                            (!_currentStreams.TryGetValue(entry.StreamIdentifier, out var entryWorker) ||
                             entryWorker.IsAborted)) {
                            _dataChannel.Reader.TryRead(out _);
                            DropDataEntry(entry);
                            continue;
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
                            try {
                                await _writeStream.WriteAsync(
                                    entry.RentedBuffer!.AsMemory(0, entry.Length), token)
                                    .ConfigureAwait(false);
                            }
                            finally {
                                ArrayPool<byte>.Shared.Return(entry.RentedBuffer!);
                            }

                            if (entry.CompletesResponse)
                                CompleteWrittenResponse(entry.StreamIdentifier);

                            didWork = true;
                            break;
                        }

                        // Gather mode: accumulate frames for batched write
                        if (gatherOffset + entry.Length > gatherBuffer.Length) {
                            // Flush current batch before it overflows
                            if (gatherOffset > 0) {
                                await FlushGatheredDataAsync(
                                    gatherBuffer, gatherOffset, completedResponses, token)
                                    .ConfigureAwait(false);
                                gatherOffset = 0;
                                didWork = true;
                            }
                        }

                        entry.RentedBuffer!.AsSpan(0, entry.Length).CopyTo(gatherBuffer.AsSpan(gatherOffset));
                        gatherOffset += entry.Length;
                        ArrayPool<byte>.Shared.Return(entry.RentedBuffer!);

                        if (entry.CompletesResponse)
                            completedResponses.Add(entry.StreamIdentifier);

                        didWork = true;
                    }

                    // Flush remaining gathered data
                    if (gatherOffset > 0) {
                        await FlushGatheredDataAsync(
                            gatherBuffer, gatherOffset, completedResponses, token)
                            .ConfigureAwait(false);
                        didWork = true;
                    }

                    // Phase 3: Flush

                    // Phase 4: Wait for signal
                    Interlocked.Exchange(ref _writeSignalState, 0);

                    // Double-check all sources before sleeping
                    if (_ringBuffer.ReadableCount > 0)
                        continue;

                    // Only re-enter the outer loop if the peeked entry can actually make
                    // progress. A DATA frame whose FlowControlledBytes exceed the current
                    // connection window cannot be consumed by Phase 2 — treating it as
                    // "work available" would spin at 100% CPU until a client WINDOW_UPDATE
                    // arrives (haga-rak/fluxzy.core#634). HandleWindowUpdate calls
                    // SignalWriteLoop after incrementing _connectionWindow, so parking on
                    // _writeSignal is the correct response.
                    if (_dataChannel.Reader.TryPeek(out var nextDataEntry)) {
                        if (nextDataEntry.FlowControlledBytes == 0 ||
                            Volatile.Read(ref _connectionWindow) >= nextDataEntry.FlowControlledBytes)
                            continue;
                    }

                    if (_pendingHeaders.Reader.TryPeek(out _))
                        continue;

                    // Check termination: all sources completed and empty
                    if (_ringBuffer.IsCompleted &&
                        _dataChannel.Reader.Completion.IsCompleted &&
                        _pendingHeaders.Reader.Completion.IsCompleted) {
                        // Final drain to catch any data that arrived between checks
                        if (_ringBuffer.ReadableCount > 0 ||
                            _dataChannel.Reader.TryPeek(out _) ||
                            _pendingHeaders.Reader.TryPeek(out _))
                            continue;

                        break;
                    }

                    if (didWork)
                        await _writeStream.FlushAsync(token).ConfigureAwait(false);

                    Interlocked.Exchange(ref _writeLoopIdleForTests, null)?.TrySetResult(null);
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
                ArrayPool<byte>.Shared.Return(gatherBuffer);

                // Return rented buffers from any remaining channel entries
                while (_dataChannel.Reader.TryRead(out var remaining)) {
                    if (remaining.RentedBuffer != null)
                        ArrayPool<byte>.Shared.Return(remaining.RentedBuffer);
                }

                // Drain leftover pending header jobs (they hold only managed memory).
                while (_pendingHeaders.Reader.TryRead(out _)) { }

                _writeHalted = true;
                Interlocked.Exchange(ref _writeLoopIdleForTests, null)?.TrySetCanceled(token);
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

        /// <summary>
        ///     HPACK-encode a pending response header and commit the bytes to the ring buffer.
        ///     Called only from the single-threaded WriteLoop, so no synchronization around the
        ///     shared HPACK dynamic table is required.
        /// </summary>
        private ServerStreamWorker? EncodePendingHeader(in PendingHeaderWrite pending)
        {
            if (!_currentStreams.TryGetValue(pending.StreamIdentifier, out var worker) ||
                worker.IsAborted)
                return null;

            var encoded = _headerEncoder.Encode(
                new HeaderEncodingJob(pending.Http11Header, pending.StreamIdentifier, 0),
                _headerEncodeBuffer, !pending.HasBody);

            _ringBuffer.Write(encoded.Span);

            return pending.HasBody ? null : worker;
        }

        private async ValueTask CompleteBodylessHeaderAsync(
            ServerStreamWorker worker, CancellationToken token)
        {
            await FlushRingBufferAsync(token).ConfigureAwait(false);

            if (worker.CompleteResponse())
                CheckoutServerStreamWorker(worker);
        }

        private void CompleteWrittenResponse(int streamIdentifier)
        {
            if (_currentStreams.TryGetValue(streamIdentifier, out var worker) &&
                worker.CompleteResponse())
                CheckoutServerStreamWorker(worker);
        }

        private async ValueTask FlushGatheredDataAsync(
            byte[] gatherBuffer, int length, List<int> completedResponses,
            CancellationToken token)
        {
            await _writeStream.WriteAsync(
                gatherBuffer.AsMemory(0, length), token).ConfigureAwait(false);

            foreach (var streamIdentifier in completedResponses)
                CompleteWrittenResponse(streamIdentifier);

            completedResponses.Clear();
        }

        private void DropDataEntry(in DataFrameEntry entry)
        {
            if (entry.RentedBuffer == null)
                return;

            ArrayPool<byte>.Shared.Return(entry.RentedBuffer);
            Interlocked.Increment(ref _droppedResponseBufferCount);
        }

        public ValueTask WriteInterimResponse(int statusCode, ReadOnlyMemory<char> reasonPhrase, int streamIdentifier, CancellationToken token)
        {
            // HTTP/2 clients do not rely on Expect: 100-continue in practice
            // (§8.1.2.2 forbids the Expect header in H2 requests), so the
            // Expect-100 bridge from issue #624 is only relevant for H1
            // downstream. If this ever needs to be implemented, it should queue
            // a separate HEADERS frame with `:status: 1xx` and no END_STREAM.
            return default;
        }

        public ValueTask WriteResponseHeader(
            ResponseHeader responseHeader, RsBuffer buffer, bool shouldClose, int streamIdentifier, ReadOnlyMemory<char> requestMethod, CancellationToken token)
        {
            if (!_currentStreams.TryGetValue(streamIdentifier, out var worker) || worker.IsAborted)
                throw new FluxzyException($"Invalid Local H2 stream : identifier {streamIdentifier}");

            // Compute hasBody on the caller thread (needs requestMethod.Span) and materialize the
            // HTTP/1.1 header representation here — GetHttp11Header() is a pure, fresh allocation
            // so it's safe to hand off. Actual HPACK encoding happens on the WriteLoop, which is
            // the sole owner of the shared HPACK dynamic table: no lock required on the hot path.
            var hasBody = responseHeader.HasResponseBody(requestMethod.Span, out _);

            if (!_pendingHeaders.Writer.TryWrite(new PendingHeaderWrite(
                    responseHeader.GetHttp11Header(), streamIdentifier, hasBody)))
                throw new IOException("HTTP/2 downstream writer is closed");

            SignalWriteLoop();

            return default;
        }

        public async ValueTask WriteResponseBody(Stream responseBodyStream,
            RsBuffer rsBuffer, bool chunked, int streamIdentifier, Response? responseForTrailers, CancellationToken token)
        {
            if (!_currentStreams.TryGetValue(streamIdentifier, out var worker)) {
                throw new FluxzyException($"Invalid Local H2 stream : identifier {streamIdentifier}");
            }

            var remoteMaxFrameSize = _h2StreamSetting.Remote.MaxFrameSize;
            using var responseTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                token, worker.ResponseAbortToken);
            var responseToken = responseTokenSource.Token;

            try {
                while (true)
                {
                    var booked = await worker.BookWindowSize(remoteMaxFrameSize, responseToken)
                        .ConfigureAwait(false);

                    if (booked == 0 || worker.IsAborted)
                        return;

                    var bodySize = Math.Min(booked, remoteMaxFrameSize);
                    var ownedWindow = booked;
                    byte[]? rentedBuffer = ArrayPool<byte>.Shared.Rent(bodySize + 9);

                    try {
                        var read = await responseBodyStream
                            .ReadAsync(rentedBuffer.AsMemory(9, bodySize), responseToken)
                            .ConfigureAwait(false);

                        if (read == 0)
                        {
                            worker.RefundWindowSize(ownedWindow);
                            ownedWindow = 0;
                            break;
                        }

                        var refund = booked - read;

                        if (refund > 0) {
                            worker.RefundWindowSize(refund);
                            ownedWindow -= refund;
                        }

                        if (worker.IsAborted)
                            return;

                        new DataFrame(HeaderFlags.None, read, streamIdentifier)
                            .WriteHeaderOnly(rentedBuffer, read);

                        var frameLength = read + 9;

                        if (!_dataChannel.Writer.TryWrite(new DataFrameEntry(
                                rentedBuffer, frameLength, read, streamIdentifier)))
                            throw new IOException("HTTP/2 downstream writer is closed");

                        rentedBuffer = null;
                        ownedWindow = 0;
                        NotifyResponseDataEnqueuedForTests();
                        SignalWriteLoop();
                    }
                    finally {
                        if (rentedBuffer != null)
                            ArrayPool<byte>.Shared.Return(rentedBuffer);

                        if (ownedWindow > 0)
                            worker.RefundWindowSize(ownedWindow);
                    }
                }
            }
            catch (OperationCanceledException) when (worker.IsAborted) {
                return;
            }
            catch (ObjectDisposedException) when (worker.IsAborted) {
                return;
            }

            if (worker.IsAborted)
                return;

            // Read trailers lazily — they are set by StreamWorker after the body pipe completes
            var trailers = responseForTrailers?.Trailers;

            if (trailers != null && trailers.Count > 0) {
                // Enqueue a trailer-encoding job; WriteLoop encodes it on its own thread (no lock).
                // Wire ordering is preserved because all DATA frames for this stream are already
                // queued ahead of this entry in the same FIFO channel.
                if (!_dataChannel.Writer.TryWrite(new DataFrameEntry(trailers, streamIdentifier)))
                    throw new IOException("HTTP/2 downstream writer is closed");

                SignalWriteLoop();
            }
            else {
                // Send 0-byte EndStream DATA frame
                var endFrameBuffer = ArrayPool<byte>.Shared.Rent(9);

                new DataFrame(HeaderFlags.EndStream, 0, streamIdentifier)
                    .WriteHeaderOnly(endFrameBuffer, 0);

                if (!_dataChannel.Writer.TryWrite(new DataFrameEntry(
                        endFrameBuffer, 9, 0, streamIdentifier, completesResponse: true))) {
                    ArrayPool<byte>.Shared.Return(endFrameBuffer);
                    throw new IOException("HTTP/2 downstream writer is closed");
                }

                SignalWriteLoop();
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
            _pendingHeaders.Writer.TryComplete();
            _exchangeChannel.Writer.TryComplete();

            foreach (var (_, worker) in _currentStreams)
            {
                worker.Dispose();
            }

            _ringBuffer.Dispose();
            _headerEncodeBuffer.Dispose();
        }
    }
}
