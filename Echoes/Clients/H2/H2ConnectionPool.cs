using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Echoes.H2.Encoder;
using Echoes.H2.Encoder.Utils;
using Echoes.Helpers;

namespace Echoes.H2
{

    public class H2ConnectionPool : IHttpConnectionPool
    {

        private static readonly byte[] Preface = System.Text.Encoding.ASCII.GetBytes("PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n");

        private static int _connectionIdCounter = 0;

        private readonly Stream _baseStream;

        private CancellationTokenSource _connectionCancellationTokenSource = new();
        
        private readonly H2StreamSetting _setting;
        private readonly Action<H2ConnectionPool> _onConnectionFaulted;

        private bool _complete;

        private readonly StreamPool _streamPool;

        private Task _innerReadTask;
        private Task _innerWriteRun;

        private readonly TaskCompletionSource<object> _waitForSettingReception = new TaskCompletionSource<object>(); 

        private Channel<WriteTask> _writerChannel;

        private SemaphoreSlim _writeSemaphore = new(1);
        private readonly SemaphoreSlim _streamCreationLock = new(1);

        // Window size of the remote 
        private WindowSizeHolder _overallWindowSizeHolder;

        public int CurrentProcessedRequest = 0;
        public int FaultedRequest = 0;
        public int TotalRequest = 0;

        private readonly H2Logger _logger;

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
                new Http11Parser(setting.Local.MaxHeaderLine,
                    ArrayPoolMemoryProvider<char>.Default) ));
        }

        public int Id { get; }

        public H2StreamSetting Setting => _setting;

        private void UpStreamChannel(ref WriteTask data)
        {
            _writerChannel.Writer.TryWrite(data);
        }

        private void ReplyPing(long opaqueData)
        {
            var pingFrame = new PingFrame(opaqueData, HeaderFlags.Ack);
            var buffer = new byte[9 + pingFrame.BodyLength];

            pingFrame.Write(buffer);

            var writeTask = new WriteTask(H2FrameType.Ping, 0, 0, 0, buffer, 0);
            UpStreamChannel(ref writeTask); 
        }

        private bool ProcessIncomingSettingFrame(SettingFrame settingFrame)
        {
            _logger.IncomingSetting(ref settingFrame);
            
            if (settingFrame.Ack)
            {
                if (!_waitForSettingReception.Task.IsCompleted)
                {
                    _waitForSettingReception.SetResult(true);
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

            // We do not throw anything here, some server like https://analytics.valiuz.com/
            // sends an identifier equals to 8 that match none of the value of rfc 7540

            // ---> old : throw new InvalidOperationException("Unknown setting type");

            return true; 
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
            CancellationToken token = _connectionCancellationTokenSource.Token; 

            await _baseStream.WriteAsync(Preface, token).ConfigureAwait(false);

            // Write setting 
            await SettingHelper.WriteSetting(
                _baseStream,
                _setting.Local, _logger, token).ConfigureAwait(false);

            _innerReadTask = InternalReadLoop(token);
            // Wait from setting reception 
            _innerWriteRun = InternalWriteLoop(token);
            
            var cancelTask = WaitForSettingReceivedOrRaiseException();

            await _waitForSettingReception.Task.ConfigureAwait(false);

            await cancelTask; 
        }
        
        private void OnGoAway(GoAwayFrame frame)
        {
            if (frame.ErrorCode != H2ErrorCode.NoError)
            {
                throw new H2Exception($"Had to goaway {frame.ErrorCode}", errorCode: frame.ErrorCode); 
            }
        }

        private void OnLoopEnd(Exception ex, bool releaseChannelItems)
        {
            _complete = true; 
            // End the connection. This operation is idempotent. 

            _logger.Trace(0, "Cleanup start");

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




            _logger.Trace(0, "Cleanup end");
        }

        private async Task InternalWriteLoop(CancellationToken token)
        {
            Exception outException = null;

            try
            {
                List<WriteTask> tasks = new List<WriteTask>();
                byte[] windowSizeBuffer = new byte[13];
                

                while (!token.IsCancellationRequested)
                {
                    tasks.Clear();
                    if (_writerChannel.Reader.TryReadAll(ref tasks))
                    {
                        foreach (var element
                                 in tasks.Where(t => t.FrameType == H2FrameType.WindowUpdate))
                        {
                            new WindowUpdateFrame(element.WindowUpdateSize, element.StreamIdentifier)
                                .Write(windowSizeBuffer);

                            _logger.OutgoingWindowUpdate(element.WindowUpdateSize, element.StreamIdentifier);

                            if (token.IsCancellationRequested)
                                break; 

                            await _baseStream.WriteAsync(windowSizeBuffer, token).ConfigureAwait(false);
                            await _baseStream.FlushAsync(token);
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
                                    .WriteAsync(writeTask.BufferBytes, token)
                                    .ConfigureAwait(false);
                                
                                _logger.OutgoingFrame(writeTask.BufferBytes);

                                await _baseStream.FlushAsync(token);
                                
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
        
        /// <summary>
        /// %Write and read has to use the same thread 
        /// </summary>
        /// <returns></returns>
        private async Task InternalReadLoop(CancellationToken token)
        {
            byte [] readBuffer = new byte[_setting.Local.MaxFrameSize];
            Exception outException = null;

            int receivedDataCount = 0; 

            try
            {
                while (!token.IsCancellationRequested)
                {
                    H2FrameReadResult frame = 
                        await H2FrameReader.ReadNextFrameAsync(_baseStream, readBuffer,
                        token).ConfigureAwait(false);

                    if (frame.IsEmpty)
                        break; 

                    _logger.IncomingFrame(ref frame);
                    
                    _streamPool.TryGetExistingActiveStream(frame.StreamIdentifier, out var activeStream);
                    
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

                        // THIS IS SO DIRTY; HeaderFrames shouldn't be a ref struct 

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

                        if (activeStream == null)
                        {
                            continue;
                        }

                        await activeStream.ReceiveBodyFragmentFromConnection(
                            frame.GetDataFrame().Buffer, 
                            frame.Flags.HasFlag(HeaderFlags.EndStream), token);

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

                    if (frame.BodyType == H2FrameType.Ping)
                    {
                        ReplyPing(frame.GetPingFrame().OpaqueData);
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



        public async ValueTask Send(
            Exchange exchange, ILocalLink _,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref CurrentProcessedRequest);
            Interlocked.Increment(ref TotalRequest);


            try
            {
                _logger.Trace(exchange, "Send start");

                await InternalSend(exchange, cancellationToken);

                _logger.Trace(exchange, "Response header received");
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref FaultedRequest);

                _logger.Trace(exchange, "Send success on error " + ex);

                OnLoopEnd(ex, true);

                throw;
            }
            finally
            {
                Interlocked.Decrement(ref CurrentProcessedRequest);
            }
        }

        private async Task InternalSend(Exchange exchange, CancellationToken callerCancellationToken)
        {
            exchange.HttpVersion = "HTTP/2";

            StreamManager activeStream;
            Task waitForHeaderSentTask;
            
            using var streamCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                callerCancellationToken,
                _connectionCancellationTokenSource.Token);

            CancellationToken streamCancellationToken = streamCancellationTokenSource.Token;

            try
            {
                activeStream =
                    await _streamPool.CreateNewStreamProcessing(
                            exchange, streamCancellationToken, _streamCreationLock,
                            streamCancellationTokenSource)
                        .ConfigureAwait(false);

               // activeStream.OR

                waitForHeaderSentTask = activeStream.EnqueueRequestHeader(exchange, streamCancellationToken);
            }
            finally
            {
                if (_streamCreationLock.CurrentCount == 0)
                    _streamCreationLock.Release();
            }

            await waitForHeaderSentTask.ConfigureAwait(false);

            exchange.Metrics.RequestHeaderSent = ITimingProvider.Default.Instant();

            await activeStream.ProcessRequestBody(exchange, streamCancellationToken);

            exchange.Metrics.RequestBodySent = ITimingProvider.Default.Instant();

            await activeStream.ProcessResponse(streamCancellationToken)
                .ConfigureAwait(false);
        }

        public Authority Authority { get; }
        
        public async ValueTask DisposeAsync()
        {
            if (_connectionCancellationTokenSource == null)
                return;

            _writerChannel?.Writer.TryComplete();

            _overallWindowSizeHolder?.Dispose();
            _overallWindowSizeHolder = null;

            _connectionCancellationTokenSource.Cancel();
            _connectionCancellationTokenSource?.Dispose();
            _connectionCancellationTokenSource = null; 

            _writeSemaphore?.Dispose();
            _writeSemaphore = null;

            await _innerReadTask.ConfigureAwait(false);
            await _innerWriteRun.ConfigureAwait(false);
        }

        public void Dispose()
        {
            if (_connectionCancellationTokenSource == null)
                return; 

            _writerChannel?.Writer.TryComplete();
            _writerChannel = null;

            _overallWindowSizeHolder?.Dispose();
            _overallWindowSizeHolder = null;

            _connectionCancellationTokenSource.Cancel();
            _connectionCancellationTokenSource?.Dispose();
            _connectionCancellationTokenSource = null;

            _writeSemaphore?.Dispose();
            _writeSemaphore = null;
            _streamCreationLock.Dispose();
        }
    }
    
}