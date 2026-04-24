// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Fluxzy.Clients.H2.Encoder;
using Fluxzy.Clients.H2.Encoder.Utils;
using Fluxzy.Clients.H2.Frames;
using Fluxzy.Core;
using Fluxzy.Misc;
using Fluxzy.Misc.ResizableBuffers;

namespace Fluxzy.Clients.H2
{
    public class H2ConnectionPool : IHttpConnectionPool
    {

        private static int _connectionIdCounter;

        private readonly Connection _connection;
        private readonly CancellationToken _connectionToken;

        private readonly Action<H2ConnectionPool>? _onConnectionFaulted;
        private readonly SemaphoreSlim _streamCreationLock = new(1);

        private readonly StreamPool _streamPool;

        private readonly Channel<WriteTask>? _writerChannel;

        private readonly Stream _baseStream;

        private volatile bool _complete;

        private readonly CancellationTokenSource _connectionCancellationTokenSource = new();

        private int _faultCallbackFired;

        private volatile bool _initDone;

        private Task? _innerReadTask;
        private Task? _innerWriteRun;

        private PeriodicTimer? _idleTimer;
        private Task? _idleMonitorTask;

        private DateTime _lastActivity = ITimingProvider.Default.Instant();

        /// <summary>
        ///     Test seam: allows tests to force the idle path of <see cref="TryIdleTeardown"/>
        ///     deterministically without resorting to reflection or wall-clock waits.
        /// </summary>
        internal DateTime LastActivity {
            get => _lastActivity;
            set => _lastActivity = value;
        }

        // Window size of the remote 
        private readonly WindowSizeHolder _overallWindowSizeHolder;

        private SemaphoreSlim? _writeSemaphore = new(1);

        public volatile bool IsDisposed;
        public volatile int TotalRequest;

        public H2ConnectionPool(
            Stream baseStream,
            H2StreamSetting setting,
            Authority authority,
            Connection connection, Action<H2ConnectionPool> onConnectionFaulted)
        {
            Id = Interlocked.Increment(ref _connectionIdCounter);

            Authority = authority;
            _baseStream = baseStream;
            Setting = setting;
            _connection = connection;
            _onConnectionFaulted = onConnectionFaulted;
            _connectionToken = _connectionCancellationTokenSource.Token;

            _overallWindowSizeHolder = new WindowSizeHolder(Setting.OverallWindowSize, 0);

            _writerChannel =
                Channel.CreateUnbounded<WriteTask>(new UnboundedChannelOptions {
                    SingleReader = true,
                    SingleWriter = false
                });

            var hPackEncoder =
                new HPackEncoder(new EncodingContext(ArrayPoolMemoryProvider<char>.Default));

            var hPackDecoder =
                new HPackDecoder(new DecodingContext(authority, ArrayPoolMemoryProvider<char>.Default));

            var headerEncoder = new HeaderEncoder(hPackEncoder, hPackDecoder, setting);

            _streamPool = new StreamPool(
                new StreamContext(
                    Id, authority, setting, 
                    headerEncoder, UpStreamChannel,
                    _overallWindowSizeHolder));
        }

        public int Id { get; }

        public H2StreamSetting Setting { get; }

        /// <summary>
        ///     True once the pool is no longer accepting new exchanges — either because the
        ///     connection has terminated (<see cref="_complete"/>) or the remote sent GOAWAY
        ///     and the pool is draining in-flight streams (<see cref="StreamPool.IsDraining"/>).
        ///     <see cref="PoolBuilder"/> uses this to route future exchanges to a fresh pool
        ///     while existing in-flight streams continue to completion on this pool.
        /// </summary>
        public bool Complete => _complete || _streamPool.IsDraining;

        public void Init()
        {
            if (_initDone)
                return;

            _initDone = true;

            //_baseStream.Write(Preface);
            SettingHelper.WriteWelcomeSettings(H2Constants.Preface, _baseStream, Setting);

            _innerReadTask = InternalReadLoop(_connectionToken);
            _innerWriteRun = InternalWriteLoop(_connectionToken);

            // Per-connection idle teardown. Replaces the centralized PoolBuilder
            // sweeper (the old async-void CheckPoolStatus loop). Tick is bounded
            // so that very large MaxIdleSeconds values still observe Cancel/Dispose
            // promptly; the lower bound keeps the test-only MaxIdleSeconds=0 case
            // ticking instead of busy-spinning.
            var tickSeconds = Math.Clamp(Setting.MaxIdleSeconds, 1, 30);
            _idleTimer = new PeriodicTimer(TimeSpan.FromSeconds(tickSeconds));
            _idleMonitorTask = MonitorIdleAsync(_idleTimer, _connectionToken);
        }

        private async Task MonitorIdleAsync(PeriodicTimer timer, CancellationToken token)
        {
            try {
                while (await timer.WaitForNextTickAsync(token).ConfigureAwait(false)) {
                    if (TryIdleTeardown())
                        return;
                }
            }
            catch (OperationCanceledException) {
                // Disposal path — expected.
            }
            catch (Exception) {
                // Never propagate from the background loop; the prior async-void
                // shape made this fatal (haga-rak/fluxzy.core#614). The teardown
                // path itself is best-effort, so swallowing is correct here.
            }
        }

        /// <summary>
        ///     Returns <c>true</c> when the monitor loop should stop — either the pool
        ///     is already torn down, or this call performed the teardown. Returns
        ///     <c>false</c> when the pool is still in use and another tick is needed.
        ///
        ///     Snapshot + teardown happen under <see cref="_streamCreationLock"/> so a
        ///     concurrent <see cref="Send"/> cannot allocate a stream id on a connection
        ///     that is about to send GOAWAY. <see cref="SemaphoreSlim.Wait(int)"/> with
        ///     timeout 0 means "yield this tick if a Send is mid-flight" — a Send in
        ///     progress is itself proof the connection is not idle.
        /// </summary>
        internal bool TryIdleTeardown()
        {
            if (_complete) return true;
            if (_streamPool.ActiveStreamCount != 0) return false;

            // Fast path: a draining pool with no in-flight streams has nothing to do —
            // tear down immediately without waiting for the idle timer. Prevents the pool
            // from lingering in the dictionary after a graceful GOAWAY drained cleanly.
            if (_streamPool.IsDraining) {
                if (!_streamCreationLock.Wait(0)) return false;
                try {
                    if (_complete) return true;
                    if (_streamPool.ActiveStreamCount != 0) return false;

                    OnLoopEnd(null, true);
                    return true;
                }
                finally {
                    _streamCreationLock.Release();
                }
            }

            var instant = ITimingProvider.Default.Instant();
            if (instant - _lastActivity <= TimeSpan.FromSeconds(Setting.MaxIdleSeconds))
                return false;

            if (!_streamCreationLock.Wait(0))
                return false;

            try {
                if (_complete) return true;
                if (_streamPool.ActiveStreamCount != 0) return false;

                instant = ITimingProvider.Default.Instant();
                if (instant - _lastActivity <= TimeSpan.FromSeconds(Setting.MaxIdleSeconds))
                    return false;

                // Only emit our own GOAWAY if the remote hasn't already sent us one.
                // IsDraining is set exclusively by StreamPool.OnRemoteGoAway.
                if (!_streamPool.IsDraining) {
                    try {
                        EmitGoAway(H2ErrorCode.NoError);
                    }
                    catch {
                        // Best-effort GOAWAY; teardown proceeds either way.
                    }
                }

                OnLoopEnd(null, true);

                return true;
            }
            finally {
                _streamCreationLock.Release();
            }
        }

        /// <summary>
        ///     Test seam: captures internal state at a single point in time so tests can
        ///     assert ordering invariants (e.g. "the writer channel must be drained before
        ///     the fault callback runs"). Not used by production code paths.
        /// </summary>
        internal H2ConnectionPoolStateSnapshot SnapshotForTests()
        {
            var pendingCount = 0;
            var channelDrainedAndClosed = true;

            if (_writerChannel != null) {
                channelDrainedAndClosed = _writerChannel.Reader.Completion.IsCompleted;

                if (_writerChannel.Reader.CanCount)
                    pendingCount = _writerChannel.Reader.Count;
            }

            return new H2ConnectionPoolStateSnapshot(
                Complete: _complete,
                CtsCancelled: _connectionCancellationTokenSource.IsCancellationRequested,
                WriterChannelDrainedAndClosed: channelDrainedAndClosed,
                WriterChannelPendingCount: pendingCount,
                Draining: _streamPool.IsDraining,
                PeerLastStreamId: _streamPool.PeerLastStreamId,
                GoAwayErrorCode: (_streamPool.GoAwayException as H2Exception)?.ErrorCode,
                ActiveStreamCount: _streamPool.ActiveStreamCount);
        }

        /// <summary>Test seam: direct access to the stream pool for unit-level assertions.</summary>
        internal StreamPool StreamPoolForTests => _streamPool;

        public async ValueTask Send(
            Exchange exchange, IDownStreamPipe _, RsBuffer buffer, ExchangeScope __,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref TotalRequest);

            try {
                exchange.Connection = _connection;

                await InternalSend(exchange, buffer, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) {

                if (ex is OperationCanceledException opex
                    && cancellationToken != default
                    && opex.CancellationToken == cancellationToken) {
                    // The caller cancels this exchange.
                    // Send a reset on stream to prevent the remote
                }

                // ConnectionCloseException here means either (a) the pool was already
                // draining/complete when the exchange arrived, or (b) this stream was
                // proactively abandoned because the server's GOAWAY LastStreamId ruled
                // it out. Neither case is a pool-level transport failure — other
                // in-flight streams on this pool may still complete. Skip OnLoopEnd.
                if (ex is not ConnectionCloseException)
                    OnLoopEnd(ex, true);

                throw;
            }
        }

        public Authority Authority { get; }

        public async ValueTask DisposeAsync()
        {
            lock (this) {
                if (IsDisposed)
                    return;

                IsDisposed = true;
            }

            _writerChannel?.Writer.TryComplete();

            _overallWindowSizeHolder?.Dispose();

            // Stop the idle monitor first: PeriodicTimer.Dispose causes any pending
            // WaitForNextTickAsync to return false, so the monitor exits cleanly
            // without observing a cancelled CTS.
            _idleTimer?.Dispose();

            _connectionCancellationTokenSource?.Cancel();
            _connectionCancellationTokenSource?.Dispose();

            // Note: do NOT null out _writeSemaphore. Nulling a mutable field that
            // other code may concurrently observe is a latent NRE hazard. CTS
            // cancellation is the canonical "writers must stop" signal.
            _writeSemaphore?.Dispose();

            if (_innerReadTask != null)
                await _innerReadTask.ConfigureAwait(false);

            if (_innerWriteRun != null)
                await _innerWriteRun.ConfigureAwait(false);

            if (_idleMonitorTask != null) {
                try {
                    await _idleMonitorTask.ConfigureAwait(false);
                }
                catch {
                    // Monitor already swallows; this is belt-and-braces.
                }
            }

            try {

                await _baseStream.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception) {
                // Ignore dispose errors
            }
        }

        private void UpStreamChannel(ref WriteTask data)
        {
            _writerChannel?.Writer.TryWrite(data);
        }

        private void EmitPing(long opaqueData)
        {
            var pingFrame = new PingFrame(opaqueData, HeaderFlags.Ack);
            var buffer = new byte[9 + pingFrame.BodyLength];

            pingFrame.Write(buffer);

            var writeTask = new WriteTask(H2FrameType.Ping, 0, 0, 0, buffer);
            UpStreamChannel(ref writeTask);
        }

        private void EmitGoAway(H2ErrorCode errorCode)
        {
            var buffer = BuildGoAwayBytes(errorCode);
            var writeTask = new WriteTask(H2FrameType.Goaway, 0, 0, 0, buffer);
            UpStreamChannel(ref writeTask);
        }

        /// <summary>
        ///     Serialise a GOAWAY frame for emission from this pool.
        ///     <para>
        ///         RFC 9113 §6.8: LastStreamId carries the last *peer-initiated* stream
        ///         id the sender processed. For a client with SETTINGS_ENABLE_PUSH=0 (our
        ///         default, see <see cref="H2StreamSetting.Local"/>.EnablePush), the peer
        ///         opens zero streams, so the correct value is 0. Writing
        ///         <c>_streamPool.LastStreamIdentifier</c> (our own outgoing last id) would
        ///         mis-signal which server-initiated streams were processed, and worse
        ///         would emit the sentinel -1 on an early GOAWAY.
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     Extracted from <see cref="EmitGoAway"/> as an <c>internal</c> test seam so
        ///     the LastStreamId=0 invariant can be asserted deterministically without
        ///     racing the writer loop (which may cancel the queued WriteTask if
        ///     <c>OnLoopEnd</c> fires before it drains).
        /// </remarks>
        internal static byte[] BuildGoAwayBytes(H2ErrorCode errorCode)
        {
            const int lastProcessedPeerStreamId = 0;
            var goAwayFrame = new GoAwayFrame(lastProcessedPeerStreamId, errorCode);
            var buffer = new byte[9 + goAwayFrame.BodyLength];

            goAwayFrame.Write(buffer);

            return buffer;
        }

        private void OnGoAway(ref GoAwayFrame frame)
        {
            // Build the cause synchronously so StreamPool.OnRemoteGoAway can surface it
            // via GoAwayException. Non-NoError error codes must never be silently dropped
            // — callers rely on the code to decide whether to retry on a fresh connection.
            var cause = frame.ErrorCode == H2ErrorCode.NoError
                ? null
                : new H2Exception($"Remote GOAWAY {frame.ErrorCode}", frame.ErrorCode);

            // Flip the pool into draining mode. The read loop keeps running so in-flight
            // streams with id <= frame.LastStreamId can finish on this pool; new exchanges
            // route to a fresh pool via Complete=true. Proactive abandonment of
            // id > frame.LastStreamId streams happens inside OnRemoteGoAway.
            _streamPool.OnRemoteGoAway(frame.LastStreamId, frame.ErrorCode, cause);
        }

        private void OnLoopEnd(Exception? ex, bool releaseChannelItems)
        {
            if (_complete)
                return;

            _complete = true;

            // End the connection. This operation is idempotent.
            
            // IMPORTANT: drive ALL of our own internal cleanup BEFORE notifying the
            // fault callback. The callback (in PoolBuilder.OnConnectionFaulted) may
            // synchronously start tearing the pool down via DisposeAsync, and the
            // synchronous prefix of DisposeAsync runs inline up to its first await.
            // If we notified first, the rest of OnLoopEnd would then be running on
            // top of partially-disposed state — the reentrance race that produced
            // the NRE in haga-rak/fluxzy.core#614.
            //
            // The new ordering guarantees: by the time the fault callback runs,
            // _complete is set, the CTS is cancelled, the writer channel is
            // completed and drained, and all pending write tasks have been
            // signalled. Disposal can then run safely without racing OnLoopEnd.

            // Transport-level failure without a prior GOAWAY: surface the cause through
            // the stream pool so any stream that's currently in Send() observes a
            // meaningful GoAwayException. No-op if a GOAWAY already recorded a cause.
            if (ex != null)
                _streamPool.OnRemoteGoAway(_streamPool.PeerLastStreamId, H2ErrorCode.InternalError, ex);

            if (!_connectionCancellationTokenSource.IsCancellationRequested)
                _connectionCancellationTokenSource.Cancel();

            if (releaseChannelItems && _writerChannel != null) {
                _writerChannel.Writer.TryComplete();

                var list = new List<WriteTask>();

                if (_writerChannel.Reader.TryReadAll(list)) {
                    foreach (var item in list) {
                        if (!item.DoneTask.IsCompleted)
                            item.CompletionSource.SetCanceled();
                    }
                }
            }

            // Notify last so the callback observes a fully-quiesced pool. Use a
            // CAS-guarded fire so concurrent teardown paths (read-loop exit racing
            // with the idle monitor, or Send-path error racing with transport close)
            // cannot double-evict the pool from PoolBuilder or double-schedule
            // disposal via ObserveDisposal.
            if (Interlocked.CompareExchange(ref _faultCallbackFired, 1, 0) == 0)
                _onConnectionFaulted?.Invoke(this);
        }

        private async Task InternalWriteLoop(CancellationToken token)
        {
            Exception? outException = null;

            try {
                var tasks = new List<WriteTask>();
                var otherTasks = new List<WriteTask>();

                while (!token.IsCancellationRequested) {
                    tasks.Clear();
                    otherTasks.Clear();

                    if (_writerChannel == null)
                        break;

                    if (_writerChannel.Reader.TryReadAll(tasks)) {
                        // Separate window updates from other frames
                        var windowUpdateCount = 0;
                        var totalOtherSize = 0;

                        for (var i = 0; i < tasks.Count; i++) {
                            var task = tasks[i];
                            if (task.FrameType == H2FrameType.WindowUpdate) {
                                windowUpdateCount++;
                            }
                            else {
                                otherTasks.Add(task);
                                totalOtherSize += task.BufferBytes.Length;
                            }
                        }

                        // Batch window updates into single write
                        if (windowUpdateCount > 0) {
                            var bufferLength = windowUpdateCount * 13;
                            var heapBuffer = ArrayPool<byte>.Shared.Rent(bufferLength);
                            var memoryBuffer = new Memory<byte>(heapBuffer).Slice(0, bufferLength);

                            for (var i = 0; i < tasks.Count; i++) {
                                var writeTask = tasks[i];
                                if (writeTask.FrameType != H2FrameType.WindowUpdate)
                                    continue;

                                new WindowUpdateFrame(writeTask.WindowUpdateSize, writeTask.StreamIdentifier)
                                    .Write(memoryBuffer.Span);

                                memoryBuffer = memoryBuffer.Slice(13);
                            }

                            await _baseStream.WriteAsync(heapBuffer, 0, bufferLength, token).ConfigureAwait(false);

                            ArrayPool<byte>.Shared.Return(heapBuffer);
                        }

                        // Sort non-window-update tasks in-place by priority
                        if (otherTasks.Count > 1) {
                            otherTasks.Sort(static (a, b) => {
                                var aIsData = a.FrameType == H2FrameType.Headers || a.FrameType == H2FrameType.Data;
                                var bIsData = b.FrameType == H2FrameType.Headers || b.FrameType == H2FrameType.Data;

                                var cmp = aIsData.CompareTo(bIsData);
                                if (cmp != 0) return cmp;

                                cmp = (a.StreamDependency == 0).CompareTo(b.StreamDependency == 0);
                                if (cmp != 0) return cmp;

                                cmp = a.StreamIdentifier.CompareTo(b.StreamIdentifier);
                                if (cmp != 0) return cmp;

                                return a.Priority.CompareTo(b.Priority);
                            });
                        }

                        // Batch all non-window-update frames into single write
                        if (otherTasks.Count > 0) {
                            var batchBuffer = ArrayPool<byte>.Shared.Rent(totalOtherSize);

                            try {
                                var offset = 0;

                                for (var i = 0; i < otherTasks.Count; i++) {
                                    var writeTask = otherTasks[i];
                                    writeTask.BufferBytes.Span.CopyTo(batchBuffer.AsSpan(offset));
                                    offset += writeTask.BufferBytes.Length;
                                }

                                await _baseStream
                                      .WriteAsync(batchBuffer, 0, totalOtherSize, token)
                                      .ConfigureAwait(false);

                                for (var i = 0; i < otherTasks.Count; i++) {
                                    otherTasks[i].OnComplete(null);
                                }
                            }
                            catch (Exception ex) when (ex is SocketException || ex is IOException) {
                                for (var i = 0; i < otherTasks.Count; i++) {
                                    otherTasks[i].OnComplete(ex);
                                }

                                throw;
                            }
                            finally {
                                ArrayPool<byte>.Shared.Return(batchBuffer);
                            }
                        }

                        _lastActivity = ITimingProvider.Default.Instant();
                    }
                    else {

                        await _baseStream.FlushAsync(token).ConfigureAwait(false);
                        // async wait
                        if (!token.IsCancellationRequested
                            && !await _writerChannel.Reader.WaitToReadAsync(token))
                            break;
                    }
                }

                await _baseStream.FlushAsync(token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) {
            }
            catch (Exception ex) {
                // We catch this exception here to throw it to the
                // caller in SendAsync() instead of Dispose() ;

                outException = ex;
            }
            finally {
                OnLoopEnd(outException, true);
            }
        }

        /// <summary>
        ///     %Write and read has to use the same thread
        /// </summary>
        /// <returns></returns>
        private async Task InternalReadLoop(CancellationToken token)
        {
            using var reader = new H2FrameStreamReader(_baseStream, Setting.Remote.MaxFrameSize);

            Exception? outException = null;

            try {
                while (!token.IsCancellationRequested) {
                    var frame = await reader.ReadNextFrameAsync(token).ConfigureAwait(false);

                    if (ProcessNewFrame(frame))
                        break;
                }
            }
            catch (OperationCanceledException) {
            }
            catch (Exception ex) {
                // Surface the cause verbatim. Previously this was gated on a
                // _goAwayInitByRemote flag to suppress the synthetic H2Exception that
                // OnGoAway used to throw; with OnGoAway no longer throwing, the gate
                // is dead and any exception reaching here is a genuine transport fault.
                outException = ex;
            }
            finally {
                OnLoopEnd(outException, false);
            }
        }

        private bool ProcessNewFrame(H2FrameReadResult frame)
        {
            if (frame.IsEmpty)
                return true;
            
            _lastActivity = ITimingProvider.Default.Instant();
            
            _streamPool.TryGetExistingActiveStream(frame.StreamIdentifier, out var activeStream);

            if (frame.BodyType == H2FrameType.Settings) {
                var indexer = 0;
                var sendAck = false;

                while (frame.TryReadNextSetting(out var settingFrame, ref indexer)) {

                    var needAck = H2Helper.ProcessIncomingSettingFrame(Setting, ref settingFrame);

                    if (settingFrame.SettingIdentifier == SettingIdentifier.SettingsInitialWindowSize) {
                        // update an existing stream window size
                        _streamPool.NotifyInitialWindowChange(settingFrame.Value);
                    }

                    sendAck = sendAck || needAck;
                }

                if (sendAck) {
                    var settingFrame = new SettingFrame(true);
                    var buffer = new byte[9];
                    settingFrame.Write(buffer);
                    var writeTask = new WriteTask(H2FrameType.Settings, 0, 0, 0, buffer);
                    UpStreamChannel(ref writeTask);
                }

                return false;
            }

            if (frame.BodyType == H2FrameType.Priority) {
                if (activeStream == null)
                    return false;

                var priorityFrame = frame.GetPriorityFrame();

                activeStream.SetPriority(ref priorityFrame);
            }

            if (frame.BodyType == H2FrameType.Headers) {
                if (activeStream == null)

                    // TODO : Notify stream error, stream already closed 
                    return false;

                var headerFrame = frame.GetHeadersFrame();

                activeStream.ReceiveHeaderFragmentFromConnection(ref headerFrame);

                return false;
            }

            if (frame.BodyType == H2FrameType.Continuation) {
                if (activeStream == null)

                    // TODO : Notify stream error, stream already closed 
                    return false;

                var continuationFrame = frame.GetContinuationFrame();

                activeStream.ReceiveHeaderFragmentFromConnection(ref continuationFrame);

                return false;
            }

            if (frame.BodyType == H2FrameType.Data) {
                if (activeStream == null)
                    return false;
                
                activeStream.ReceiveBodyFragmentFromConnection(
                    frame.GetDataFrame().Buffer,
                    frame.Flags.HasFlag(HeaderFlags.EndStream));

                return false;
            }

            if (frame.BodyType == H2FrameType.RstStream) {
                if (activeStream == null)
                    return false;

                activeStream.ResetRequest(frame.GetRstStreamFrame().ErrorCode);

                return false;
            }

            if (frame.BodyType == H2FrameType.WindowUpdate) {
                var windowSizeIncrement = frame.GetWindowUpdateFrame().WindowSizeIncrement;

                if (activeStream == null) {
                    _overallWindowSizeHolder.UpdateWindowSize(windowSizeIncrement);

                    return false;
                }

                activeStream.NotifyStreamWindowUpdate(windowSizeIncrement);

                return false;
            }

            if (frame.BodyType == H2FrameType.Ping) {
                EmitPing(frame.GetPingFrame().OpaqueData);

                return false;
            }

            if (frame.BodyType == H2FrameType.Goaway) {
                var goAwayFrame = frame.GetGoAwayFrame();

                OnGoAway(ref goAwayFrame);

                // Do NOT break the read loop here. In-flight streams with id <= LastStreamId
                // are still expected to receive HEADERS/DATA/RST/trailers from the server.
                // The loop exits naturally when the server half-closes the transport or the
                // idle monitor's drain fast path fires once draining completes.
                return false;
            }

            return false;
        }

        internal readonly record struct H2ConnectionPoolStateSnapshot(
            bool Complete,
            bool CtsCancelled,
            bool WriterChannelDrainedAndClosed,
            int WriterChannelPendingCount,
            bool Draining = false,
            int PeerLastStreamId = int.MaxValue,
            H2ErrorCode? GoAwayErrorCode = null,
            int ActiveStreamCount = 0);

        private async ValueTask InternalSend(
            Exchange exchange, RsBuffer buffer,
            CancellationToken callerCancellationToken)
        {
            exchange.HttpVersion = "HTTP/2";

            StreamWorker? activeStream = null;

            // CTS for stream-scoped operations. Owned by this method unless
            // transferred to a background request-body task (see below).
            var streamCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                callerCancellationToken,
                _connectionToken);

            var streamCancellationToken = streamCancellationTokenSource.Token;
            var ownsTokenSource = true; // tracks disposal responsibility

            try {
                Task waitForHeaderSentTask;

                try {
                    if (Complete || _connectionToken.IsCancellationRequested)
                        throw new ConnectionCloseException("This connection is already closed");

                    activeStream =
                        await _streamPool.CreateNewStreamProcessing(
                                             exchange, streamCancellationToken, _streamCreationLock,
                                             streamCancellationTokenSource)
                                         .ConfigureAwait(false);

                    // activeStream.OR

                    waitForHeaderSentTask =
                        activeStream.EnqueueRequestHeader(exchange, buffer, streamCancellationToken);
                }
                finally {
                    if (_streamCreationLock.CurrentCount == 0)
                        _streamCreationLock.Release();
                }

                await waitForHeaderSentTask.ConfigureAwait(false);

                exchange.Metrics.RequestHeaderSent = ITimingProvider.Default.Instant();

                var hasRequestBody = exchange.Request.Body != null
                    && (!exchange.Request.Body.CanSeek || exchange.Request.Body.Length > 0);

                if (!hasRequestBody) {
                    exchange.Metrics.RequestBodySent = exchange.Metrics.RequestHeaderSent;
                }

                // Run request body upload and response processing concurrently.
                // HTTP/2 allows the server to send response headers and data before
                // the request body is complete (required for gRPC bidirectional streaming).

                // Allocate a dedicated buffer for request body forwarding so we can
                // return the shared buffer to the caller once the response is available.
                var bodyBuffer = RsBuffer.Allocate(
                    Math.Min(Setting.Local.MaxFrameSize, buffer.Buffer.Length));

                var requestBodyTask = activeStream.ProcessRequestBody(
                    exchange, bodyBuffer, streamCancellationToken);

                try {
                    await activeStream.ProcessResponse(streamCancellationToken, this)
                                      .ConfigureAwait(false);
                }
                catch {
                    // Observe the request body task to prevent UnobservedTaskException
                    try { await requestBodyTask.ConfigureAwait(false); }
                    catch { /* already failing on ProcessResponse */ }

                    bodyBuffer.Dispose();
                    throw;
                }

                if (requestBodyTask.IsCompleted) {
                    // Fast path: request body finished before or with the response.
                    bodyBuffer.Dispose();

                    try {
                        await requestBodyTask.ConfigureAwait(false);
                    }
                    catch when (exchange.Response.Header != null) {
                        // Response already received — request body errors are benign
                    }
                }
                else {
                    // Slow path (gRPC bidirectional streaming): the client may wait for
                    // the response before half-closing the request stream. Returning now
                    // lets the orchestrator forward the response to the client, which
                    // unblocks the client to finish sending. The request body task
                    // continues in the background with its own buffer and CTS.
                    ownsTokenSource = false; // background task takes ownership

                    _ = requestBodyTask.AsTask().ContinueWith(_ => {
                        bodyBuffer.Dispose();
                        streamCancellationTokenSource.Dispose();
                    }, TaskScheduler.Default);
                }

                // RequestBodySent is set inside ProcessRequestBody for with-body requests,
                // and set to RequestHeaderSent for no-body requests (above).
            }
            catch (OperationCanceledException opex) {
                // Abandonment-by-GOAWAY surfaces here as OCE because AbandonAsRetryable
                // cancels the stream CTS. Rewrap as ConnectionCloseException so the
                // orchestrator (ProxyOrchestrator.cs:524) treats it as retryable and
                // Send() skips the pool-level OnLoopEnd teardown.
                if (activeStream != null && activeStream.AbandonedByGoAway) {
                    throw new ConnectionCloseException(
                        "Stream abandoned after remote GOAWAY; request was not processed",
                        activeStream.AbandonInnerCause);
                }

                if (activeStream != null &&
                    opex.CancellationToken == callerCancellationToken)

                    // The caller cancels this exchange.
                    // Send a reset on stream to prevent the remote
                    // from sending further data
                    activeStream.ResetByCaller();

                throw;
            }
            finally {
                if (ownsTokenSource) {
                    if (!streamCancellationTokenSource.IsCancellationRequested)
                        streamCancellationTokenSource.Cancel();

                    streamCancellationTokenSource.Dispose();
                }
            }
        }
    }
}
