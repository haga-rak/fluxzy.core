using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Echoes.H2.Helpers;

namespace Echoes.H2
{
    public class H2ClientConnection : IAsyncDisposable, IDisposable
    { 
        private static readonly byte[] Preface = System.Text.Encoding.ASCII.GetBytes("PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n");

        private readonly Stream _baseStream;
        private readonly IH2FrameReader _streamReader;

        private  CancellationTokenSource _connectionCancellationTokenSource = new CancellationTokenSource();
        private readonly CancellationTokenSource _settingReceptionCancellationTokenSource = new CancellationTokenSource();
        private readonly H2StreamSetting _setting;

        private readonly StreamPool _statePool;

        private Task _innerReadTask;
        private Task _innerWriteRun;
        private TaskCompletionSource<object> _waitForSettingReception = new TaskCompletionSource<object>(); 

        private Channel<WriteTask> _writerChannel;

        private SemaphoreSlim _writeSemaphore = new SemaphoreSlim(1);
        private readonly StreamProcessingBuilder _streamProcessingBuilder;

        private WindowSizeHolder _overallWindowSizeHolder; 

        private H2ClientConnection(
            Stream baseStream,
            H2StreamSetting setting
            )
        {
            _baseStream = baseStream;
            _streamReader = new H2Reader();
            _setting = setting;

            _overallWindowSizeHolder = new WindowSizeHolder(_setting.OverallWindowSize);

            _streamProcessingBuilder = new StreamProcessingBuilder(_connectionCancellationTokenSource.Token,
                UpStreamChannel,
                _setting, _overallWindowSizeHolder, ArrayPool<byte>.Shared
            );

            _writerChannel =
                Channel.CreateUnbounded<WriteTask>(new UnboundedChannelOptions()
                {
                    SingleReader = true,
                    SingleWriter = false
                });

            _statePool = new StreamPool(_streamProcessingBuilder, _setting.Remote);
        }

        private void UpStreamChannel(ref WriteTask data)
        {
            _writerChannel.Writer.TryWrite(data);
        }

        public H2StreamSetting Setting => _setting;
        
        private bool ProcessIncomingSettingFrame(SettingFrame settingFrame)
        {
            Logger.WriteLine(settingFrame.ToString());
            
            if (settingFrame.Ack)
            {
                if (!_waitForSettingReception.Task.IsCompleted)
                {
                    _waitForSettingReception.SetResult(true);
                    _settingReceptionCancellationTokenSource.Cancel(false);
                }

                return false;
            }

            switch (settingFrame.SettingIdentifier)
            {
                case SettingIdentifier.SettingsEnablePush:
                    if (settingFrame.Value > 0)
                    {
                        // TODO Close connection on error. Push not supported 
                        return false;
                    }
                    _setting.Remote.EnablePush = false;
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

            throw new InvalidOperationException("Unknow setting type");
        }

        private async Task RaiseExceptionIfSettingNotReceived()
        {
            try
            {
                await Task.Delay(_setting.WaitForSettingDelay, _settingReceptionCancellationTokenSource.Token);

                if (!_waitForSettingReception.Task.IsCompleted)
                    _waitForSettingReception.SetException(new H2Exception(
                        $"Server settings was not received under {(int)_setting.WaitForSettingDelay.TotalMilliseconds}."));
            }
            catch (TaskCanceledException)
            {

            }
        }

        private async Task WriteSetting()
        {
            byte[] settingBuffer = new byte[16];
            int written = new SettingFrame(SettingIdentifier.SettingsEnablePush, 0).Write(settingBuffer);

            await _baseStream.WriteAsync(settingBuffer, 0, written);

            //written = new SettingFrame(SettingIdentifier.SettingsInitialWindowSize, _setting.OverallWindowSize).Write(settingBuffer);
            //await _baseStream.WriteAsync(settingBuffer, 0, written);
        }

        private async Task WriteAckSetting()
        {
            byte[] settingBuffer = new byte[16]; 
            int written = new SettingFrame(true).Write(settingBuffer);

            await _baseStream.WriteAsync(settingBuffer, 0, written);

        }

        private async Task Init()
        {
            await _baseStream.WriteAsync(Preface, _connectionCancellationTokenSource.Token).ConfigureAwait(false);

            // Write setting 
            await WriteSetting().ConfigureAwait(false);

            _innerReadTask = InternalReadingLoop();
            // Wait from setting reception 
            _innerWriteRun = InternalWriteLoop();
            
            var cancelTask = RaiseExceptionIfSettingNotReceived();

            await _waitForSettingReception.Task.ConfigureAwait(false);
            await cancelTask; 
        }

        private void OnGoAway(GoAwayFrame frame)
        {
            if (frame.ErrorCode != H2ErrorCode.NoError)
            {
                throw new H2Exception($"Had to goaway {frame.ErrorCode}"); 
            }

            Logger.WriteLine($"Goaway : Error code {frame.ErrorCode} : LastStreamId {frame.LastStreamId}");
        }

        private async Task InternalWriteLoop()
        {
            try
            {
                IList<WriteTask> tasks = new List<WriteTask>();
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
                                if (writeTask.FrameType == H2FrameType.Data)
                                {

                                }

                                await _baseStream
                                    .WriteAsync(writeTask.BufferBytes, _connectionCancellationTokenSource.Token)
                                    .ConfigureAwait(false);

                                await _baseStream.FlushAsync(_connectionCancellationTokenSource.Token);
                                Logger.WriteLine(
                                    $"Sending {writeTask.BufferBytes.Length} on streamId {writeTask.StreamIdentifier}");

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
                        if (!await _writerChannel.Reader.WaitToReadAsync(_connectionCancellationTokenSource.Token))
                        {
                            break;
                        }
                    }

                }
            }
            catch (OperationCanceledException)
            {
                // Natural death ; 
            }
            catch (Exception ex) when (ex is SocketException || ex is IOException)
            {
                throw;
            }
            finally
            {
                _connectionCancellationTokenSource?.Cancel();
            }
        }

        /// <summary>
        /// %Write and read has to use the same thread 
        /// </summary>
        /// <returns></returns>
        private async Task InternalReadingLoop()
        {
            byte [] readBuffer = new byte[_setting.Local.MaxFrameSize];

            while (!_connectionCancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    var frame = await _streamReader.ReadNextFrameAsync(_baseStream, readBuffer,
                        _connectionCancellationTokenSource.Token).ConfigureAwait(false);

                    _statePool.TryGetExistingActiveStream(frame.StreamIdentifier, out var activeStream);
                    

                    Logger.WriteLine($"Receiving {frame.BodyType} ({frame.BodyLength}) on streamId {frame.StreamIdentifier} / Flags : {frame.Flags}");

                    if (frame.BodyType == H2FrameType.Settings)
                    {
                        var settingReceived = ProcessIncomingSettingFrame(frame.GetSettingFrame());

                        if (settingReceived)
                            await WriteAckSetting().ConfigureAwait(false);

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

                        activeStream.NotifyRemoteWindowUpdate(windowSizeIncrement);
                        
                        continue;
                    }

                    if (frame.BodyType == H2FrameType.Goaway)
                    {
                        OnGoAway(frame.GetGoAwayFrame());
                        continue;
                    }
                }
                catch
                {
                    _connectionCancellationTokenSource?.Cancel();
                    throw;
                }
            }
        }
        
        public async Task<H2Message> Send(
            ReadOnlyMemory<char> requestHeader, 
            Stream requestBodyStream,
            long bodyLength = -1,
            CancellationToken cancellationToken = default)
        {
            var activeStream = await _statePool.CreateNewStreamActivity(cancellationToken).ConfigureAwait(false);

            await activeStream.ProcessRequest(requestHeader, requestBodyStream, bodyLength)
                .ConfigureAwait(false);

            return await activeStream.ProcessResponse(cancellationToken)
                .ConfigureAwait(false);
        }

        public static async Task<H2ClientConnection> Open(Stream stream, H2StreamSetting setting)
        {
            var connection = new H2ClientConnection(stream, setting);

            await connection.Init();
            return connection; 
        }

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
        }
    }
}