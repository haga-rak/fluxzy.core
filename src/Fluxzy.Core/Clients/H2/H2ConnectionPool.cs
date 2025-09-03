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

        private readonly H2Logger _logger;
        private readonly Action<H2ConnectionPool>? _onConnectionFaulted;
        private readonly SemaphoreSlim _streamCreationLock = new(1);

        private readonly StreamPool _streamPool;

        private readonly Channel<WriteTask>? _writerChannel;

        private readonly Stream _baseStream;

        private volatile bool _complete;

        private readonly CancellationTokenSource _connectionCancellationTokenSource = new();

        private bool _goAwayInitByRemote;

        private volatile bool _initDone;

        private Task? _innerReadTask;
        private Task? _innerWriteRun;

        private DateTime _lastActivity = ITimingProvider.Default.Instant();

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
            _logger = new H2Logger(Authority, Id);
            _connectionToken = _connectionCancellationTokenSource.Token;

            _overallWindowSizeHolder = new WindowSizeHolder(_logger, Setting.OverallWindowSize, 0);

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
                    Id, authority, setting, _logger,
                    headerEncoder, UpStreamChannel,
                    _overallWindowSizeHolder));
        }

        public int Id { get; }

        public H2StreamSetting Setting { get; }

        public bool Complete => _complete;

        public void Init()
        {
            if (_initDone)
                return;

            _initDone = false;

            //_baseStream.Write(Preface);
            SettingHelper.WriteWelcomeSettings(H2Constants.Preface, _baseStream, Setting, _logger);

            _innerReadTask = InternalReadLoop(_connectionToken);
            _innerWriteRun = InternalWriteLoop(_connectionToken);
        }

        public ValueTask<bool> CheckAlive()
        {
            var instant = ITimingProvider.Default.Instant();

            if (!_complete && _streamPool.ActiveStreamCount == 0 &&
                instant - _lastActivity > TimeSpan.FromSeconds(Setting.MaxIdleSeconds)) {
                if (!_goAwayInitByRemote) {
                    try {
                        EmitGoAway(H2ErrorCode.NoError);
                    }
                    catch {
                        // Ignore go away error
                    }

                    OnLoopEnd(null, true);

                    _logger.Trace(0, () => "IDLE timeout. Connection closed.");
                }

                return new ValueTask<bool>(false);
            }

            return new ValueTask<bool>(true);
        }

        public async ValueTask Send(
            Exchange exchange, IDownStreamPipe _, RsBuffer buffer, ExchangeScope __,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref TotalRequest);

            try {
                _logger.Trace(exchange, "Send start");

                exchange.Connection = _connection;

                await InternalSend(exchange, buffer, cancellationToken).ConfigureAwait(false);

                _logger.Trace(exchange, "Response header received");
            }
            catch (Exception ex) {
                _logger.Trace(exchange, "Send on error " + ex);

                if (ex is OperationCanceledException opex
                    && cancellationToken != default
                    && opex.CancellationToken == cancellationToken) {
                    // The caller cancels this exchange. 
                    // Send a reset on stream to prevent the remote 
                }

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

            _logger.Trace(0, () => "Disposed");
            _connectionCancellationTokenSource?.Cancel();
            _connectionCancellationTokenSource?.Dispose();

            _writeSemaphore?.Dispose();
            _writeSemaphore = null;

            if (_innerReadTask != null)
                await _innerReadTask.ConfigureAwait(false);

            if (_innerWriteRun != null)
                await _innerWriteRun.ConfigureAwait(false);

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
            var goAwayFrame = new GoAwayFrame(_streamPool.LastStreamIdentifier, errorCode);
            var buffer = new byte[9 + goAwayFrame.BodyLength];

            goAwayFrame.Write(buffer);

            var writeTask = new WriteTask(H2FrameType.Goaway, 0, 0, 0, buffer);
            UpStreamChannel(ref writeTask);
        }

        private void OnGoAway(ref GoAwayFrame frame)
        {
            _goAwayInitByRemote = true;

            if (frame.ErrorCode == H2ErrorCode.CompressionError)
                Console.WriteLine("Compression error");

            if (frame.ErrorCode != H2ErrorCode.NoError)
                throw new H2Exception($"Had to goaway {frame.ErrorCode}", frame.ErrorCode);
        }

        private void OnLoopEnd(Exception? ex, bool releaseChannelItems)
        {
            if (_complete)
                return;

            _complete = true;

            // End the connection. This operation is idempotent. 

            _logger.Trace(0, "Cleanup start " + ex);

            if (_onConnectionFaulted != null)
                _onConnectionFaulted(this);

            if (ex != null)
                _streamPool.OnGoAway(ex);

            if (!_connectionCancellationTokenSource.IsCancellationRequested)
                _connectionCancellationTokenSource?.Cancel();

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

            _logger.Trace(0, "Cleanup end");
        }

        private async Task InternalWriteLoop(CancellationToken token)
        {
            Exception? outException = null;

            try {
                var tasks = new List<WriteTask>();

                while (!token.IsCancellationRequested) {
                    tasks.Clear();

                    if (_writerChannel == null)
                        break;

                    if (_writerChannel.Reader.TryReadAll(tasks)) {
                        var windowUpdateTasks = tasks.Where(t => t.FrameType == H2FrameType.WindowUpdate).ToArray();

                        if (windowUpdateTasks.Length > 0) {
                            var bufferLength = windowUpdateTasks.Length * 13;
                            var heapBuffer = ArrayPool<byte>.Shared.Rent(bufferLength);
                            var memoryBuffer = new Memory<byte>(heapBuffer).Slice(0, bufferLength);

                            try {
                                foreach (var writeTask in windowUpdateTasks) {
                                    new WindowUpdateFrame(writeTask.WindowUpdateSize, writeTask.StreamIdentifier)
                                        .Write(memoryBuffer.Span);

                                    memoryBuffer = memoryBuffer.Slice(13);

                                    _logger.OutgoingWindowUpdate(writeTask.WindowUpdateSize,
                                        writeTask.StreamIdentifier);
                                }
                            }
                            finally {
                                ArrayPool<byte>.Shared.Return(heapBuffer);
                            }

                            await _baseStream.WriteAsync(heapBuffer, 0, bufferLength, token).ConfigureAwait(false);
                        }

                        var count = 0;
                        var totalSize = 0;

                        // TODO improve the priority rule 
                        foreach (var writeTask in tasks
                                                  .Where(t => t.FrameType != H2FrameType.WindowUpdate)
                                                  .OrderBy(
                                                      r => r.FrameType == H2FrameType.Headers ||
                                                           r.FrameType == H2FrameType.Data)
                                                  .ThenBy(r => r.StreamDependency == 0)
                                                  .ThenBy(r => r.StreamIdentifier)
                                                  .ThenBy(r => r.Priority)
                                ) {
                            try {

                                await _baseStream
                                      .WriteAsync(writeTask.BufferBytes, token)
                                      .ConfigureAwait(false);

                                _logger.OutgoingFrame(writeTask.BufferBytes);

                                totalSize += writeTask.BufferBytes.Length;

                                count++;

                                // await _baseStream.FlushAsync(token);
                                // _lastActivity = ITimingProvider.Default.Instant();
                                writeTask.OnComplete(null);
                            }
                            catch (Exception ex) when (ex is SocketException || ex is IOException) {
                                writeTask.OnComplete(ex);

                                throw;
                            }
                        }
                    }
                    else {
                        // async wait 
                        if (!token.IsCancellationRequested
                            && !await _writerChannel.Reader.WaitToReadAsync(token))
                            break;
                    }
                }
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
            using var readBuffer = MemoryPool<byte>.Shared.Rent(Setting.Remote.MaxFrameSize);

            Exception? outException = null;

            try {
                while (!token.IsCancellationRequested) {
                    _logger.TraceDeep(0, () => "1");
                    
                    var frame =
                        await H2FrameReader.ReadNextFrameAsync(_baseStream, readBuffer.Memory,
                            token).ConfigureAwait(false);

                    if (ProcessNewFrame(frame))
                        break;
                }

                _logger.TraceDeep(0, () => "Natural death");
            }
            catch (OperationCanceledException) {
                _logger.TraceDeep(0, () => "OperationCanceledException death");
            }
            catch (Exception ex) {
                if (!_goAwayInitByRemote)
                    outException = ex;
            }
            finally {
                OnLoopEnd(outException, false);
            }
        }

        private bool ProcessNewFrame(H2FrameReadResult frame)
        {
            _logger.TraceDeep(0, () => "2");

            if (frame.IsEmpty)
                return true;

            _logger.TraceDeep(0, () => "3");

            _lastActivity = ITimingProvider.Default.Instant();

            _logger.IncomingFrame(ref frame);

            _streamPool.TryGetExistingActiveStream(frame.StreamIdentifier, out var activeStream);

            if (frame.BodyType == H2FrameType.Settings) {
                var indexer = 0;
                var sendAck = false;

                while (frame.TryReadNextSetting(out var settingFrame, ref indexer)) {

                    _logger.IncomingSetting(ref settingFrame);
                    var needAck = H2Helper.ProcessIncomingSettingFrame(Setting, ref settingFrame);

                    if (settingFrame.SettingIdentifier == SettingIdentifier.SettingsInitialWindowSize) {
                        // update an existing stream window size
                        _streamPool.NotifyInitialWindowChange(settingFrame.Value);
                    }

                    sendAck = sendAck || needAck;

                    _logger.TraceDeep(0, () => "4");
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
                _logger.TraceDeep(0, () => "5");

                if (activeStream == null)
                    return false;

                var priorityFrame = frame.GetPriorityFrame();

                activeStream.SetPriority(ref priorityFrame);
            }

            if (frame.BodyType == H2FrameType.Headers) {
                _logger.TraceDeep(0, () => "6");

                if (activeStream == null)

                    // TODO : Notify stream error, stream already closed 
                    return false;

                var headerFrame = frame.GetHeadersFrame();

                activeStream.ReceiveHeaderFragmentFromConnection(ref headerFrame);

                return false;
            }

            if (frame.BodyType == H2FrameType.Continuation) {
                _logger.TraceDeep(0, () => "7");

                if (activeStream == null)

                    // TODO : Notify stream error, stream already closed 
                    return false;

                var continuationFrame = frame.GetContinuationFrame();

                activeStream.ReceiveHeaderFragmentFromConnection(ref continuationFrame);

                return false;
            }

            if (frame.BodyType == H2FrameType.Data) {
                _logger.TraceDeep(0, () => "8 : ");

                if (activeStream == null)
                    return false;

                _logger.TraceDeep(0, () => "8 : " + activeStream.StreamIdentifier);

                activeStream.ReceiveBodyFragmentFromConnection(
                    frame.GetDataFrame().Buffer,
                    frame.Flags.HasFlag(HeaderFlags.EndStream));

                _logger.TraceDeep(0, () => "8 1 : " + activeStream.StreamIdentifier);

                return false;
            }

            if (frame.BodyType == H2FrameType.RstStream) {
                _logger.TraceDeep(0, () => "9");

                if (activeStream == null)
                    return false;

                activeStream.ResetRequest(frame.GetRstStreamFrame().ErrorCode);

                return false;
            }

            if (frame.BodyType == H2FrameType.WindowUpdate) {
                _logger.TraceDeep(0, () => "10");

                var windowSizeIncrement = frame.GetWindowUpdateFrame().WindowSizeIncrement;

                if (activeStream == null) {
                    _overallWindowSizeHolder.UpdateWindowSize(windowSizeIncrement);

                    return false;
                }

                activeStream.NotifyStreamWindowUpdate(windowSizeIncrement);

                return false;
            }

            if (frame.BodyType == H2FrameType.Ping) {
                _logger.TraceDeep(0, () => "11");

                EmitPing(frame.GetPingFrame().OpaqueData);

                return false;
            }

            if (frame.BodyType == H2FrameType.Goaway) {
                _logger.TraceDeep(0, () => "12");

                var goAwayFrame = frame.GetGoAwayFrame();

                OnGoAway(ref goAwayFrame);

                return goAwayFrame.ErrorCode != H2ErrorCode.NoError;
            }

            return false;
        }

        private async ValueTask InternalSend(
            Exchange exchange, RsBuffer buffer,
            CancellationToken callerCancellationToken)
        {
            exchange.HttpVersion = "HTTP/2";

            StreamWorker? activeStream = null;

            using var streamCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                callerCancellationToken,
                _connectionToken);

            var streamCancellationToken = streamCancellationTokenSource.Token;

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

                await activeStream.ProcessRequestBody(exchange, buffer, streamCancellationToken).ConfigureAwait(false);

                exchange.Metrics.RequestBodySent = ITimingProvider.Default.Instant();

                await activeStream.ProcessResponse(streamCancellationToken, this)
                                  .ConfigureAwait(false);
            }
            catch (OperationCanceledException opex) {
                if (activeStream != null &&
                    opex.CancellationToken == callerCancellationToken)

                    // The caller cancels this exchange. 
                    // Send a reset on stream to prevent the remote 
                    // from sending further data 
                    activeStream.ResetByCaller();

                throw;
            }
            finally {
                if (!streamCancellationTokenSource.IsCancellationRequested)
                    streamCancellationTokenSource.Cancel();
            }
        }
    }
}
