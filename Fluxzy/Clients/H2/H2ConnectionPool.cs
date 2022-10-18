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
using Fluxzy.Misc;

namespace Fluxzy.Clients.H2
{

    public class H2ConnectionPool : IHttpConnectionPool
    {
        private static readonly byte[] Preface = System.Text.Encoding.ASCII.GetBytes("PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n");

        private static int _connectionIdCounter = 0;

        private Stream _baseStream;

        private CancellationTokenSource _connectionCancellationTokenSource = new();
        
        private readonly H2StreamSetting _setting;
        private readonly Connection _connection;
        private readonly Action<H2ConnectionPool>?  _onConnectionFaulted;

        private volatile bool _complete;

        private readonly StreamPool _streamPool;

        private Task? _innerReadTask;
        private Task? _innerWriteRun;

        private readonly TaskCompletionSource<bool> _waitForSettingReception = new(); 

        private Channel<WriteTask>? _writerChannel;

        private SemaphoreSlim? _writeSemaphore = new(1);
        private readonly SemaphoreSlim _streamCreationLock = new(1);

        // Window size of the remote 
        private WindowSizeHolder _overallWindowSizeHolder;

        public int CurrentProcessedRequest = 0;
        public int FaultedRequest = 0;
        public int TotalRequest = 0;

        private readonly H2Logger _logger;

        private DateTime _lastActivity = ITimingProvider.Default.Instant();

        private Exception _loopEndException = null;
        private bool _goAwayInitByRemote;

        public H2ConnectionPool(
            Stream baseStream,
            H2StreamSetting setting,
            Authority authority, 
            Connection connection, Action<H2ConnectionPool> onConnectionFaulted
            )
        {
            Id = Interlocked.Increment(ref _connectionIdCounter); 

            Authority = authority;
            _baseStream = baseStream;
            _setting = setting;
            _connection = connection;
            _onConnectionFaulted = onConnectionFaulted;
            _logger = new H2Logger(Authority, Id); 

            _overallWindowSizeHolder = new WindowSizeHolder(_logger, _setting.OverallWindowSize,0);

            _writerChannel =
                Channel.CreateUnbounded<WriteTask>(new UnboundedChannelOptions()
                {
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
                _overallWindowSizeHolder, 
                new Http11Parser(setting.Local.MaxHeaderLine) ));
        }

        public int Id { get; }

        public H2StreamSetting Setting => _setting;

        private void UpStreamChannel(ref WriteTask data)
        {
            _writerChannel!.Writer.TryWrite(data);
        }

        private void EmitPing(long opaqueData)
        {
            var pingFrame = new PingFrame(opaqueData, HeaderFlags.Ack);
            var buffer = new byte[9 + pingFrame.BodyLength];

            pingFrame.Write(buffer);

            var writeTask = new WriteTask(H2FrameType.Ping, 0, 0, 0, buffer, 0);
            UpStreamChannel(ref writeTask); 
        }

        private void EmitGoAway(H2ErrorCode errorCode)
        {
            var goAwayFrame = new GoAwayFrame(_streamPool.LastStreamIdentifier,errorCode);
            var buffer = new byte[9 + goAwayFrame.BodyLength];

            goAwayFrame.Write(buffer);

            var writeTask = new WriteTask(H2FrameType.Goaway, 0, 0, 0, buffer, 0);
            UpStreamChannel(ref writeTask); 
        }

        private bool ProcessIncomingSettingFrame(SettingFrame settingFrame)
        {
            _logger.IncomingSetting(ref settingFrame);
            
            if (settingFrame.Ack)
            {
                if (!_waitForSettingReception.Task.IsCompleted)
                {
                    _waitForSettingReception.TrySetResult(true);
                }

                return false;
            }
            
            switch (settingFrame.SettingIdentifier)
            {
                case SettingIdentifier.SettingsEnablePush:
                    if (settingFrame.Value > 0)
                    {
                        // TODO Send a Goaway. Push not supported 
                        return false;
                    }
                    return true;
                case SettingIdentifier.SettingsMaxConcurrentStreams:
                    _setting.Remote.SettingsMaxConcurrentStreams = settingFrame.Value;
                    return true;

                case SettingIdentifier.SettingsInitialWindowSize:
                    _setting.OverallWindowSize = settingFrame.Value;
                    return true;

                case SettingIdentifier.SettingsMaxFrameSize:
                    _setting.Remote.MaxFrameSize = settingFrame.Value;
                    return true;

                case SettingIdentifier.SettingsMaxHeaderListSize: 
                    _setting.Remote.MaxHeaderListSize = settingFrame.Value;
                    return true;

                case SettingIdentifier.SettingsHeaderTableSize:
                    _setting.SettingsHeaderTableSize = settingFrame.Value;
                    return true;
            }

            // We do not throw anything here, some server  
            // sends an identifier equals to 8 that match none of the value of rfc 7540

            // ---> old : throw new InvalidOperationException("Unknown setting type");

            return true; 
        }

        private async void RaiseExceptionIfSettingNotReceived()
        {
            try
            {
                await Task.Delay(_setting.WaitForSettingDelay, _connectionCancellationTokenSource.Token);
            }
            catch (TaskCanceledException)
            {
                // Main connection was ended before receiving final setting acquittement 

                _waitForSettingReception.TrySetResult(false);
                return; 
            }

            if (!_waitForSettingReception.Task.IsCompleted)
            {
                _waitForSettingReception.SetException(new H2Exception(
                    $"Server settings was not received under {(int)_setting.WaitForSettingDelay.TotalMilliseconds}.",
                    H2ErrorCode.SettingsTimeout, null));
            }
        }

        private Task<bool> WaitForSettingReceivedOrRaiseException()
        {
            RaiseExceptionIfSettingNotReceived();

            return _waitForSettingReception.Task;
        }

        public bool Complete => _complete;

        public async ValueTask Init()
        {
            var token = _connectionCancellationTokenSource.Token; 

            await _baseStream.WriteAsync(Preface, token).ConfigureAwait(false);

            // Write setting 
            await SettingHelper.WriteSetting(
                _baseStream,
                _setting.Local, _logger, token).ConfigureAwait(false);

            _innerReadTask = InternalReadLoop(token);
            // Wait from setting reception 
            
            var waitSettingTask = WaitForSettingReceivedOrRaiseException();

            await _waitForSettingReception.Task.ConfigureAwait(false);

            var settingReceived = await waitSettingTask;

            _innerWriteRun = InternalWriteLoop(token);

            if (!settingReceived)
            {
                throw new IOException("Connection closed before receiving settings. More information on InnerException.",
                    _loopEndException);
            }
        }

        public ValueTask<bool> CheckAlive()
        {
            var instant = ITimingProvider.Default.Instant();

            if (!_complete && _streamPool.ActiveStreamCount == 0 && 
                (instant - _lastActivity) > TimeSpan.FromSeconds(_setting.MaxIdleSeconds))
            {
                if (!_goAwayInitByRemote)
                {
                    try
                    {
                        EmitGoAway(H2ErrorCode.NoError);
                    }
                    catch
                    {
                        // Ignore go away error
                    }

                    OnLoopEnd(null, true);

                    _logger.Trace(0, () => $"IDLE timeout. Connection closed.");
                }

                return new ValueTask<bool>(false); 
            }

            return new ValueTask<bool>(true); 
        }

        private void OnGoAway(GoAwayFrame frame)
        {
            _goAwayInitByRemote = true; 

            if (frame.ErrorCode != H2ErrorCode.NoError)
            {
                throw new H2Exception($"Had to goaway {frame.ErrorCode}", errorCode: frame.ErrorCode); 
            }

        }
        
        private void OnLoopEnd(Exception? ex, bool releaseChannelItems)
        {
            if (_complete)
                return; 

            _complete = true; 
            // End the connection. This operation is idempotent. 

            _logger.Trace(0, "Cleanup start");

            if (_onConnectionFaulted != null)
                _onConnectionFaulted(this);

            if (ex != null)
            {
                _streamPool.OnGoAway(ex);
                _loopEndException = ex; 
            }

            _connectionCancellationTokenSource?.Cancel();

            if (releaseChannelItems && _writerChannel != null)
            {
                _writerChannel.Writer.TryComplete();

                var list = new List<WriteTask>();

                if (_writerChannel.Reader.TryReadAll(ref list))
                {
                    foreach (var item in list)
                    {
                        if (!item.DoneTask.IsCompleted)
                        {
                            item.CompletionSource.SetCanceled();
                        }
                    }
                }
            }

            _logger.Trace(0, "Cleanup end");
        }

        private async Task InternalWriteLoop(CancellationToken token)
        {
            Exception? outException = null;

            try
            {
                var tasks = new List<WriteTask>();
                var windowSizeBuffer = new byte[13];
                

                while (!token.IsCancellationRequested)
                {
                    tasks.Clear();
                    if (_writerChannel.Reader.TryReadAll(ref tasks))
                    {
                        var windowUpdateTasks = tasks.Where(t => t.FrameType == H2FrameType.WindowUpdate).ToArray();

                        if (windowUpdateTasks.Length > 0)
                        {
                            var bufferLength = windowUpdateTasks.Length * 13;
                            var buffer = ArrayPool<byte>.Shared.Rent(bufferLength);
                            var memoryBuffer = new Memory<byte>(buffer).Slice(0, bufferLength);

                            try
                            {
                                foreach (var writeTask in windowUpdateTasks)
                                {
                                    new WindowUpdateFrame(writeTask.WindowUpdateSize, writeTask.StreamIdentifier)
                                        .Write(memoryBuffer.Span);

                                    memoryBuffer = memoryBuffer.Slice(13);

                                    _logger.OutgoingWindowUpdate(writeTask.WindowUpdateSize, writeTask.StreamIdentifier);
                                }
                            }
                            finally
                            {
                                ArrayPool<byte>.Shared.Return(buffer);
                            }
                            
                            await _baseStream.WriteAsync(buffer,0, bufferLength, token).ConfigureAwait(false);
                        }

                        int count = 0;
                        int totalSize = 0; 

                        // TODO improve the priority rule 
                        foreach (var writeTask in tasks
                                     .Where(t => t.FrameType != H2FrameType.WindowUpdate)
                                     .OrderBy(
                                         r => r.FrameType == H2FrameType.Headers || r.FrameType == H2FrameType.Data)
                                     .ThenBy(r => r.StreamDependency == 0)
                                     .ThenBy(r => r.StreamIdentifier)
                                     .ThenBy(r => r.Priority)
                                )
                        {
                            try
                            {
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
                            catch (Exception ex) when (ex is SocketException || ex is IOException)
                            {
                                writeTask.OnComplete(ex);
                                throw;
                            }
                        }

                        if (count > 1)
                        {
                            Console.WriteLine($"Accumlated frames : {count} / {totalSize}");
                        }
                    }
                    else
                    {
                        // async wait 
                        if (!token.IsCancellationRequested 
                            && !await _writerChannel.Reader.WaitToReadAsync(token))
                        {
                            break;
                        }
                    }

                }
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception ex)
            {
                // We catch this exception here to throw it to the
                // caller in SendAsync() instead of Dispose() ;

                outException = ex; 
            }
            finally
            {
                OnLoopEnd(outException, true);
            }
        }


        private bool EvaluateCond()
        {
            _logger.TraceDeep(0, () => "deadlock?");
            return true; 
        }

        /// <summary>
        /// %Write and read has to use the same thread 
        /// </summary>
        /// <returns></returns>
        private async Task InternalReadLoop(CancellationToken token)
        {
            byte[] readBuffer = ArrayPool<byte>.Shared.Rent(_setting.Remote.MaxFrameSize);
            
            Exception outException = null;

            try
            {
                while (EvaluateCond() && !token.IsCancellationRequested)
                {

                    _logger.TraceDeep(0,() => "1");

                    H2FrameReadResult frame = 
                        await H2FrameReader.ReadNextFrameAsync(_baseStream, readBuffer,
                        token).ConfigureAwait(false);

                    _logger.TraceDeep(0, () => "2");

                    if (frame.IsEmpty)
                        break;

                    _logger.TraceDeep(0, () => "3");

                    _lastActivity = ITimingProvider.Default.Instant();

                    _logger.IncomingFrame(ref frame);
                    
                    _streamPool.TryGetExistingActiveStream(frame.StreamIdentifier, out var activeStream);
                    
                    if (frame.BodyType == H2FrameType.Settings)
                    {
                        var settingReceived = ProcessIncomingSettingFrame(frame.GetSettingFrame());


                        _logger.TraceDeep(0, () => "4");

                        if (settingReceived)
                            await SettingHelper.WriteAckSetting(_baseStream).ConfigureAwait(false);
                        
                        if (_setting.Remote.MaxFrameSize != readBuffer.Length)
                        {
                            // Update of max frae 

                            if (_setting.Remote.MaxFrameSize > _setting.MaxFrameSizeAllowed)
                            {

                                _logger.Trace(0, () => $"Server required max frame size is larger than MaxFrameSizeAllowed = {_setting.MaxFrameSizeAllowed}");

                                _setting.Remote.MaxFrameSize = _setting.MaxFrameSizeAllowed;
                            }

                            readBuffer = new byte[_setting.Remote.MaxFrameSize];

                            _logger.Trace(0, () => $"max frame size updated to {_setting.Remote.MaxFrameSize}");
                        }

                        continue;
                    }

                    if (frame.BodyType == H2FrameType.Priority)
                    {
                        _logger.TraceDeep(0, () => "5");

                        if (activeStream == null)
                        {
                            continue;
                        }

                        activeStream.SetPriority(frame.GetPriorityFrame());
                    }

                    if (frame.BodyType == H2FrameType.Headers)
                    {
                        _logger.TraceDeep(0, () => "6");

                        if (activeStream == null)
                        {
                            // TODO : Notify stream error, stream already closed 
                            continue;
                        }

                        // TODO: THIS IS SO DIRTY; HeaderFrames shouldn't be a ref struct 

                        activeStream.ReceiveHeaderFragmentFromConnection(
                            frame.GetHeadersFrame().BodyLength,
                            frame.GetHeadersFrame().EndStream,
                            frame.GetHeadersFrame().EndHeaders,
                            frame.GetHeadersFrame().Data
                            );

                        continue;
                    }

                    if (frame.BodyType == H2FrameType.Continuation)
                    {
                        _logger.TraceDeep(0, () => "7");

                        if (activeStream == null)
                        {
                            // TODO : Notify stream error, stream already closed 
                            continue;
                        }

                        // THIS IS SO DIRTY; ContinuationFrame shouldn't be a ref struct 

                        activeStream.ReceiveHeaderFragmentFromConnection(
                            frame.GetContinuationFrame().BodyLength,
                            frame.GetContinuationFrame().EndHeaders,
                            frame.GetContinuationFrame().Data
                            );

                        continue;
                    }

                    if (frame.BodyType == H2FrameType.Data)
                    {

                        _logger.TraceDeep(0, () => "8 : ");
                        if (activeStream == null)
                        {
                            continue;
                        }

                        _logger.TraceDeep(0, () => "8 : " + activeStream.StreamIdentifier);

                        await activeStream.ReceiveBodyFragmentFromConnection(
                            frame.GetDataFrame().Buffer, 
                            frame.Flags.HasFlag(HeaderFlags.EndStream), token);


                        _logger.TraceDeep(0, () => "8 1 : " + activeStream.StreamIdentifier);

                        continue;
                    }

                    if (frame.BodyType == H2FrameType.RstStream)
                    {
                        _logger.TraceDeep(0, () => "9");

                        if (activeStream == null)
                        {
                            continue;
                        }

                        activeStream.ResetRequest(frame.GetRstStreamFrame().ErrorCode);
                        continue;
                    }

                    if (frame.BodyType == H2FrameType.WindowUpdate)
                    {
                        _logger.TraceDeep(0, () => "10");

                        var windowSizeIncrement = frame.GetWindowUpdateFrame().WindowSizeIncrement;

                        if (activeStream == null)
                        {
                            _overallWindowSizeHolder.UpdateWindowSize(windowSizeIncrement);
                            continue;
                        }

                        activeStream.NotifyStreamWindowUpdate(windowSizeIncrement);

                        continue;
                    }

                    if (frame.BodyType == H2FrameType.Ping)
                    {
                        _logger.TraceDeep(0, () => "11");

                        EmitPing(frame.GetPingFrame().OpaqueData);
                        continue;
                    }

                    if (frame.BodyType == H2FrameType.Goaway)
                    {
                        _logger.TraceDeep(0, () => "12");

                        OnGoAway(frame.GetGoAwayFrame());
                        break;
                    }
                }

                _logger.TraceDeep(0, () => "Natural death");
            }
            catch (OperationCanceledException)
            {

                _logger.TraceDeep(0, () => "OperationCanceledException death");
            }
            catch (Exception ex)
            {
                outException = ex;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(readBuffer);
                OnLoopEnd(outException, false);
            }
        }



        public async ValueTask Send(
            Exchange exchange, ILocalLink _, byte[] buffer,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref CurrentProcessedRequest);
            Interlocked.Increment(ref TotalRequest);

            try
            {
                _logger.Trace(exchange, "Send start");

                exchange.Connection = _connection; 

                await InternalSend(exchange, buffer, cancellationToken);

                _logger.Trace(exchange, "Response header received");
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref FaultedRequest);
                _logger.Trace(exchange, "Send on error " + ex);


                if (ex is OperationCanceledException opex
                    && cancellationToken != default
                    && opex.CancellationToken == cancellationToken)
                {
                    // The caller cancels this exchange. 
                    // Send a reset on stream to prevent the remote 
                }

                OnLoopEnd(ex, true);

                throw;
            }
            finally
            {
                Interlocked.Decrement(ref CurrentProcessedRequest);
            }
        }

        private async ValueTask InternalSend(Exchange exchange, byte[] buffer, CancellationToken callerCancellationToken)
        {
            exchange.HttpVersion = "HTTP/2";

            StreamWorker activeStream = null;

            using var streamCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                callerCancellationToken,
                _connectionCancellationTokenSource.Token);

            CancellationToken streamCancellationToken = streamCancellationTokenSource.Token;

            try
            {
                Task waitForHeaderSentTask;

                try
                {
                    if (Complete || _connectionCancellationTokenSource.Token.IsCancellationRequested)
                        throw new ConnectionCloseException("This connection is already closed");

                    activeStream =
                        await _streamPool.CreateNewStreamProcessing(
                                exchange, streamCancellationToken, _streamCreationLock,
                                streamCancellationTokenSource)
                            .ConfigureAwait(false);

                    // activeStream.OR

                    waitForHeaderSentTask = activeStream.EnqueueRequestHeader(exchange, buffer, streamCancellationToken);
                }
                finally
                {
                    if (_streamCreationLock.CurrentCount == 0)
                        _streamCreationLock.Release();
                }

                await waitForHeaderSentTask;

                exchange.Metrics.RequestHeaderSent = ITimingProvider.Default.Instant();

                await activeStream.ProcessRequestBody(exchange, buffer, streamCancellationToken);

                exchange.Metrics.RequestBodySent = ITimingProvider.Default.Instant();

                await activeStream.ProcessResponse(streamCancellationToken)
                    .ConfigureAwait(false);

            }
            catch (OperationCanceledException opex)
            {
                if (activeStream != null &&
                    opex.CancellationToken == callerCancellationToken)
                {

                    // The caller cancels this exchange. 
                    // Send a reset on stream to prevent the remote 
                    // from sending further data 

                    activeStream.ResetByCaller();
                }

                throw;
            }
            finally
            {
                if (!streamCancellationTokenSource.IsCancellationRequested)
                {
                    streamCancellationTokenSource.Cancel();
                }
            }
        }

        public Authority Authority { get; }
        
        public async ValueTask DisposeAsync()
        {
            if (_connectionCancellationTokenSource == null)
                return;

            _writerChannel?.Writer.TryComplete();

            _overallWindowSizeHolder?.Dispose();
            _overallWindowSizeHolder = null;


            _logger.Trace(0, () => "Disposed");
            _connectionCancellationTokenSource.Cancel();
            _connectionCancellationTokenSource?.Dispose();
            _connectionCancellationTokenSource = null; 

            _writeSemaphore?.Dispose();
            _writeSemaphore = null;

            if (_innerReadTask != null)
                await _innerReadTask.ConfigureAwait(false);

            if (_innerWriteRun != null)
                await _innerWriteRun.ConfigureAwait(false);

            if (_baseStream != null)
            {
                await _baseStream.DisposeAsync();
                _baseStream = null;
            }
        }

        public void Dispose()
        {
            if (_connectionCancellationTokenSource == null)
                return; 

            _writerChannel?.Writer.TryComplete();
            _writerChannel = null;

            _overallWindowSizeHolder?.Dispose();
            _overallWindowSizeHolder = null;

            _logger.Trace(0, () => "Disposed");
            _connectionCancellationTokenSource.Cancel();
            _connectionCancellationTokenSource?.Dispose();
            _connectionCancellationTokenSource = null;

            _writeSemaphore?.Dispose();
            _writeSemaphore = null;
            _streamCreationLock.Dispose();

            if (_baseStream != null)
            {
                _baseStream.Dispose();
                _baseStream = null; 
            }
        }
    }
    
}