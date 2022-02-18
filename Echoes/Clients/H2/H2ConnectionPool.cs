using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Echoes.H2.Encoder.Utils;
using Echoes.Helpers;

namespace Echoes.H2
{
    public class H2ConnectionPool : IHttpConnectionPool
    { 
        private static readonly byte[] Preface = System.Text.Encoding.ASCII.GetBytes("PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n");

        private readonly Stream _baseStream;
        private readonly IH2FrameReader _streamReader;

        private CancellationTokenSource _connectionCancellationTokenSource = new();
        
        private readonly H2StreamSetting _setting;
        private readonly Action<H2ConnectionPool> _onConnectionFaulted;

        private bool _complete;

        private readonly StreamPool _streamPool;

        private Task _innerReadTask;
        private Task _innerWriteRun;
        private readonly TaskCompletionSource<object> _waitForSettingReception = new TaskCompletionSource<object>(); 

        private Channel<WriteTask> _writerChannel;

        private SemaphoreSlim _writeSemaphore = new SemaphoreSlim(1);
        private readonly SemaphoreSlim _streamCreationLock = new SemaphoreSlim(1);

        private readonly StreamProcessingBuilder _streamProcessingBuilder;

        // Window size of the remote 
        private WindowSizeHolder _overallWindowSizeHolder; 

        public H2ConnectionPool(
            Stream baseStream,
            H2StreamSetting setting,
            Authority authority, 
            Connection connection, Action<H2ConnectionPool> onConnectionFaulted
            )
        {
            Authority = authority;
            _baseStream = baseStream;
            _streamReader = new H2Reader();
            _setting = setting;
            _onConnectionFaulted = onConnectionFaulted;

            _overallWindowSizeHolder = new WindowSizeHolder(_setting.OverallWindowSize,0);

            _streamProcessingBuilder = new StreamProcessingBuilder(authority, _connectionCancellationTokenSource.Token,
                UpStreamChannel,
                _setting, _overallWindowSizeHolder, ArrayPool<byte>.Shared, new Http11Parser(setting.MaxHeaderSize, 
                    new ArrayPoolMemoryProvider<char>())
            );

            _writerChannel =
                Channel.CreateUnbounded<WriteTask>(new UnboundedChannelOptions()
                {
                    SingleReader = true,
                    SingleWriter = false
                });

            _streamPool = new StreamPool(_streamProcessingBuilder, _setting.Remote);
        }

        public H2StreamSetting Setting => _setting;

        private void UpStreamChannel(ref WriteTask data)
        {
            _writerChannel.Writer.TryWrite(data);
        }
        
        private bool ProcessIncomingSettingFrame(SettingFrame settingFrame)
        {
            if (DebugContext.EnableNetworkFileDump)
                Logger.WriteLine(settingFrame.ToString());
            
            if (settingFrame.Ack)
            {
                if (!_waitForSettingReception.Task.IsCompleted)
                {
                    _waitForSettingReception.SetResult(true);
                }

                return false;
            }

            Console.WriteLine($"Received : {settingFrame.SettingIdentifier} : value = {settingFrame.Value} - aut = {Authority.HostName}");

            if (
                settingFrame.SettingIdentifier != SettingIdentifier.SettingsMaxConcurrentStreams
               // &&  settingFrame.SettingIdentifier != SettingIdentifier.SettingsInitialWindowSize
                )
            {
                
            }

            switch (settingFrame.SettingIdentifier)
            {
                
                case SettingIdentifier.SettingsEnablePush:
                    if (settingFrame.Value > 0)
                    {
                        // TODO Close connection on error. Push not supported 
                        return false;
                    }
                    //_setting.Remote.EnablePush = false;
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

            throw new InvalidOperationException("Unknown setting type");
        }

        private async void RaiseExceptionIfSettingNotReceived()
        {
            await Task.Delay(_setting.WaitForSettingDelay);

            if (!_waitForSettingReception.Task.IsCompleted)
            {
                _waitForSettingReception.SetException(new H2Exception(
                    $"Server settings was not received under {(int)_setting.WaitForSettingDelay.TotalMilliseconds}.",
                    H2ErrorCode.SettingsTimeout, null));
            }
        }

        private async Task WaitForSettingReceivedOrRaiseException()
        {
            RaiseExceptionIfSettingNotReceived();
            await _waitForSettingReception.Task;
            //await taskValidation; 
        }

        public bool Faulted  {
            get
            {
                return _complete;
            }
        }

        public async Task Init()
        {
            await _baseStream.WriteAsync(Preface, _connectionCancellationTokenSource.Token).ConfigureAwait(false);

            // Write setting 
            await SettingHelper.WriteSetting(_baseStream,
                _setting.Local, _connectionCancellationTokenSource.Token).ConfigureAwait(false);

            _innerReadTask = InternalReadLoop();
            // Wait from setting reception 
            _innerWriteRun = InternalWriteLoop();
            
            var cancelTask = WaitForSettingReceivedOrRaiseException();

            await _waitForSettingReception.Task.ConfigureAwait(false);

            await cancelTask; 
        }
        
        private void OnGoAway(GoAwayFrame frame)
        {
           //  Logger.WriteLine($"Goaway : Error code {frame.ErrorCode} : LastStreamId {frame.LastStreamId}");

            if (frame.ErrorCode != H2ErrorCode.NoError)
            {
                throw new H2Exception($"Had to goaway {frame.ErrorCode}", errorCode: frame.ErrorCode); 
            }
            else
            {

            }

        }

        private void OnLoopEnd(Exception ex, bool releaseChannelItems)
        {
            _complete = true; 
            // End the connection. This operation is idempotent. 

            _onConnectionFaulted(this);

            if (ex != null)
            {
                _streamPool.OnGoAway(ex);
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
        }

        private async Task InternalWriteLoop()
        {
            Exception outException = null;

            try
            {
                List<WriteTask> tasks = new List<WriteTask>();

                byte[] windowSizeBuffer = new byte[13];

                while (true)
                {
                    tasks.Clear();
                    if (_writerChannel.Reader.TryReadAll(ref tasks))
                    {
                        foreach (var element
                                 in tasks.Where(t => t.FrameType == H2FrameType.WindowUpdate)
                                     .GroupBy(f => f.StreamIdentifier))
                        {
                            var streamId = element.Key;
                            var updateValue = element.Sum(e => e.WindowUpdateSize);

                            new WindowUpdateFrame(updateValue, streamId).Write(windowSizeBuffer);

                            if (DebugContext.EnableNetworkFileDump)
                            Logger.WriteLine(
                                $"Sending WindowUpdate : {updateValue} on {streamId} Merge : {element.Count()}");

                            await _baseStream.WriteAsync(windowSizeBuffer, _connectionCancellationTokenSource.Token)
                                .ConfigureAwait(false);
                            await _baseStream.FlushAsync(_connectionCancellationTokenSource.Token);
                            
                        }

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
                                    .WriteAsync(writeTask.BufferBytes, _connectionCancellationTokenSource.Token)
                                    .ConfigureAwait(false);

                                await _baseStream.FlushAsync(_connectionCancellationTokenSource.Token);

                                Logger.WriteLine(writeTask);

                                if (DebugContext.EnableNetworkFileDump)
                                    if (writeTask.FrameType == H2FrameType.Data)
                                    Logger.WriteLine("Overall window state = " +
                                                     _overallWindowSizeHolder.WindowSize);

                                writeTask.OnComplete(null);
                            }
                            catch (Exception ex) when (ex is SocketException || ex is IOException)
                            {
                                writeTask.OnComplete(ex);
                                throw;
                            }
                        }
                    }
                    else
                    {
                        // async wait 
                        if (!_connectionCancellationTokenSource.IsCancellationRequested 
                            && !await _writerChannel.Reader.WaitToReadAsync(_connectionCancellationTokenSource.Token))
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
        
        /// <summary>
        /// %Write and read has to use the same thread 
        /// </summary>
        /// <returns></returns>
        private async Task InternalReadLoop()
        {
            byte [] readBuffer = new byte[_setting.Local.MaxFrameSize];
            Exception outException = null;

            try
            {
                while (_connectionCancellationTokenSource != null && !_connectionCancellationTokenSource.IsCancellationRequested)
                {
                    H2FrameReadResult frame = await _streamReader.ReadNextFrameAsync(_baseStream, readBuffer,
                        _connectionCancellationTokenSource.Token).ConfigureAwait(false);

                    // var str = frame.ToString();
                    
                    _streamPool.TryGetExistingActiveStream(frame.StreamIdentifier, out var activeStream);

                    Logger.WriteLine(ref frame, _overallWindowSizeHolder);

                    //Logger.WriteLine(
                    //    $"Receiving {frame.BodyType} ({frame.BodyLength}) on streamId {frame.StreamIdentifier} / Flags : {frame.Flags}");

                    if (frame.BodyType == H2FrameType.Settings)
                    {
                        var settingReceived = ProcessIncomingSettingFrame(frame.GetSettingFrame());

                        if (settingReceived)
                            await SettingHelper.WriteAckSetting(_baseStream).ConfigureAwait(false);

                        continue;
                    }

                    if (frame.BodyType == H2FrameType.Priority)
                    {
                        if (activeStream == null)
                        {
                            continue;
                        }

                        activeStream.SetPriority(frame.GetPriorityFrame());
                    }

                    if (frame.BodyType == H2FrameType.Headers)
                    {
                        if (activeStream == null)
                        {
                            // TODO : Notify stream error, stream already closed 
                            continue;
                        }

                        activeStream.ReceiveHeaderFragmentFromConnection(frame.GetHeadersFrame());
                        continue;
                    }

                    if (frame.BodyType == H2FrameType.Continuation)
                    {
                        if (activeStream == null)
                        {
                            // TODO : Notify stream error, stream already closed 
                            continue;
                        }

                        activeStream.ReceiveHeaderFragmentFromConnection(frame.GetContinuationFrame());
                        continue;
                    }

                    if (frame.BodyType == H2FrameType.Data)
                    {
                        if (activeStream == null)
                        {
                            continue;
                        }

                        activeStream.ReceiveBodyFragmentFromConnection(
                            frame.GetDataFrame().Buffer, frame.Flags.HasFlag(HeaderFlags.EndStream));

                        continue;
                    }

                    if (frame.BodyType == H2FrameType.RstStream)
                    {
                        if (activeStream == null)
                        {
                            continue;
                        }

                        activeStream.ResetRequest(frame.GetRstStreamFrame().ErrorCode);
                        continue;
                    }

                    if (frame.BodyType == H2FrameType.WindowUpdate)
                    {
                        var windowSizeIncrement = frame.GetWindowUpdateFrame().WindowSizeIncrement;

                        if (activeStream == null)
                        {
                            _overallWindowSizeHolder.UpdateWindowSize(windowSizeIncrement);
                            continue;
                        }

                        activeStream.NotifyStreamWindowUpdate(windowSizeIncrement);

                        continue;
                    }

                    if (frame.BodyType == H2FrameType.Goaway)
                    {
                        OnGoAway(frame.GetGoAwayFrame());
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                outException = ex;
            }
            finally
            {
                OnLoopEnd(outException, false);
            }
        }

        public int CurrentProcessedRequest = 0; 
        public int FaultedRequest = 0; 
        public int TotalRequest = 0; 
        
        public async ValueTask Send(
            Exchange exchange, ILocalLink _,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref CurrentProcessedRequest);
            Interlocked.Increment(ref TotalRequest);


            try
            {
                await InternalSend(exchange, cancellationToken);
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref FaultedRequest);
                OnLoopEnd(ex, true);
                throw;
            }
            finally
            {
                Interlocked.Decrement(ref CurrentProcessedRequest);
            }
        }

        private async Task InternalSend(Exchange exchange, CancellationToken cancellationToken)
        {
            exchange.HttpVersion = "HTTP/2";

            StreamProcessing activeStream;
            Task waitForHeaderSentTask;

            try
            {
                activeStream =
                    await _streamPool.CreateNewStreamProcessing(exchange, cancellationToken, _streamCreationLock)
                        .ConfigureAwait(false);

                waitForHeaderSentTask = activeStream.EnqueueRequestHeader(exchange);
            }
            finally
            {
                if (_streamCreationLock.CurrentCount == 0)
                    _streamCreationLock.Release();
            }

            await waitForHeaderSentTask.ConfigureAwait(false);

            exchange.Metrics.RequestHeaderSent = ITimingProvider.Default.Instant();

            await activeStream.ProcessRequestBody(exchange);

            exchange.Metrics.RequestBodySent = ITimingProvider.Default.Instant();

            var h2Message = await activeStream.ProcessResponse(cancellationToken)
                .ConfigureAwait(false);

            exchange.Response.Body = h2Message.ResponseStream;
        }

        public Authority Authority { get; }
        
        public async ValueTask DisposeAsync()
        {
            _writerChannel?.Writer.TryComplete();
            _writerChannel = null; 


            _overallWindowSizeHolder?.Dispose();
            _overallWindowSizeHolder = null; 

            _connectionCancellationTokenSource?.Dispose();
            _connectionCancellationTokenSource = null; 

            _writeSemaphore?.Dispose();
            _writeSemaphore = null;

            await _innerReadTask.ConfigureAwait(false);
            await _innerWriteRun.ConfigureAwait(false);
        }

        public void Dispose()
        {
            _writerChannel?.Writer.TryComplete();
            _writerChannel = null;

            _overallWindowSizeHolder?.Dispose();
            _overallWindowSizeHolder = null;

            _connectionCancellationTokenSource?.Dispose();
            _connectionCancellationTokenSource = null;

            _writeSemaphore?.Dispose();
            _writeSemaphore = null;
            _streamCreationLock.Dispose();
        }
    }
    
}