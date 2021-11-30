using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Echoes.H2.Cli.Helpers;

namespace Echoes.H2.Cli
{
    public class H2ClientConnection : IAsyncDisposable
    { 
        

        private static readonly byte[] Preface = System.Text.Encoding.ASCII.GetBytes("PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n");

        private readonly Stream _baseStream;
        private readonly IH2FrameReader _streamReader;
        private readonly IH2StreamWriter _streamWriter;

        private readonly CancellationTokenSource _connectionCancellationTokenSource = new CancellationTokenSource();
        private readonly H2StreamSetting _setting;

        private readonly StreamPool _statePool;

        private Task _innerReadTask;
        private Task _innerWriteRun;
        private TaskCompletionSource<object> _waitForSettingReception = new TaskCompletionSource<object>(); 

        private readonly Channel<WriteTask> _writerChannel;

        private readonly SemaphoreSlim _writeSemaphore = new SemaphoreSlim(1);
        private readonly StreamProcessingBuilder _streamProcessingBuilder;

        private readonly WindowSizeHolder _overallWindowSizeHolder; 

        private H2ClientConnection(
            Stream baseStream,
            H2StreamSetting setting
            )
        {
            _baseStream = baseStream;
            _streamReader = new H2Reader();
            _setting = setting;

            _overallWindowSizeHolder = new WindowSizeHolder(_setting.Remote.WindowSize);

            _streamProcessingBuilder = new StreamProcessingBuilder(_connectionCancellationTokenSource.Token,
                UpStreamChannel,
                _setting, _overallWindowSizeHolder, ArrayPool<byte>.Shared
            );

            _writerChannel =
                Channel.CreateBounded<WriteTask>(new BoundedChannelOptions(16)
                {
                    SingleReader = true,
                    SingleWriter = true
                });

            _statePool = new StreamPool(_streamProcessingBuilder, _setting.Remote);
        }

        private async ValueTask UpStreamChannel(WriteTask data, CancellationToken callerCancellationToken)
        {
            await _writerChannel.Writer.WriteAsync(data).ConfigureAwait(false);
        }

        public H2StreamSetting Setting => _setting;
        
        private bool ProcessIncomingSettingFrame(SettingFrame settingFrame)
        {
            Logger.WriteLine(settingFrame.ToString());
            ;

            if (settingFrame.Ack)
            {
                if (!_waitForSettingReception.Task.IsCompleted)
                    _waitForSettingReception.SetResult(true);

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
                    _setting.Remote.WindowSize = settingFrame.Value;
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
            await Task.Delay(_setting.WaitForSettingDelay);

            if (!_waitForSettingReception.Task.IsCompleted)
                _waitForSettingReception.SetException(new H2Exception($"Server settings was not received under {(int) _setting.WaitForSettingDelay.TotalMilliseconds}."));
        }

        private async Task WriteSetting()
        {
            byte[] settingBuffer = new byte[16];
            int written = new SettingFrame(SettingIdentifier.SettingsEnablePush, 0).Write(settingBuffer);
            await _baseStream.WriteAsync(settingBuffer, 0, written);
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

        private void BreakOnGoAway(GoAwayFrame frame)
        {

        }


        private async Task InternalWriteLoop()
        {
            try
            {
                IList<WriteTask> tasks = new List<WriteTask>();

                while (true)
                {
                    tasks.Clear();
                    if (_writerChannel.Reader.TryReadAll(ref tasks))
                    {
                        // TODO improve the priority rule 
                        foreach (var writeTask in tasks
                                     .OrderBy(r => r.StreamDependency == 0)
                                     .ThenBy(r => r.StreamIdentifier)
                                     .ThenBy(r => r.Priority)
                                 )
                        {
                            try
                            {
                                await _baseStream.WriteAsync(writeTask.BufferBytes, _connectionCancellationTokenSource.Token).ConfigureAwait(false);
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
        }

        /// <summary>
        /// %Write and read has to use the same thread 
        /// </summary>
        /// <returns></returns>
        private async Task InternalReadingLoop()
        {
            byte [] readBuffer = new byte[_setting.ReadBufferLength];
            var settingReceived = false; 

            while (!_connectionCancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    var frame = await _streamReader.ReadNextFrameAsync(_baseStream, readBuffer,
                        _connectionCancellationTokenSource.Token).ConfigureAwait(false);

                    _statePool.TryGetExistingActiveStream(frame.StreamIdentifier, out var activeStream);

                    if (frame.BodyType == H2FrameType.Settings)
                    {
                        settingReceived = ProcessIncomingSettingFrame(frame.GetSettingFrame());

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

                        await
                            activeStream.ReceiveBodyFragmentFromConnection(
                                    frame.GetDataFrame().Buffer, frame.Flags.HasFlag(HeaderFlags.EndStream))
                                .ConfigureAwait(false);

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
                        if (activeStream == null)
                        {
                            _overallWindowSizeHolder.UpdateWindowSize(frame.GetWindowUpdateFrame().WindowSizeIncrement);
                            continue;
                        }

                        activeStream.NotifyWindowUpdate(frame.GetWindowUpdateFrame().WindowSizeIncrement);
                        
                        continue;
                    }

                    if (frame.BodyType == H2FrameType.Goaway)
                    {
                        BreakOnGoAway(frame.GetGoAwayFrame());
                        continue;
                    }
                }
                catch
                {
                    throw;
                }
            }
        }
        
        public async Task<H2Message> Send(
            ReadOnlyMemory<char> requestHeader, 
            Stream requestBodyStream, 
            int bufferLength = 16 * 1024, 
            CancellationToken cancellationToken = default)
        {
            using var activeStream = await _statePool.CreateNewStreamActivity(cancellationToken).ConfigureAwait(false);

            await activeStream.ProcessRequest(requestHeader, requestBodyStream)
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
            _writerChannel.Writer.Complete();
            _overallWindowSizeHolder.Dispose();

            _connectionCancellationTokenSource.Dispose();
            _writeSemaphore.Dispose();

            await _innerReadTask.ConfigureAwait(false);
            await _innerWriteRun.ConfigureAwait(false);
        }
    }


    public class H2Exception : Exception
    {
        public H2Exception(string message) :
            base(message)
        {

        }
    }

    public class H2Stream
    {

    }

    public class StreamState
    {
        public StreamStateType StateType { get; set; }

        public int WindowSize { get; set; }
    }


    public static class Logger
    {
        public static void WriteLine(object line)
        {
            Console.WriteLine(line);
        }
        public static void WriteLine(string line)
        {
            Console.WriteLine(line);
        }
    }

}