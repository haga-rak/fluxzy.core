using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
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

        private H2ClientConnection(
            Stream baseStream,
            H2StreamSetting setting
            )
        {
            _baseStream = baseStream;
            _streamReader = new H2Reader();
            _setting = setting;

            _writerChannel =
                Channel.CreateBounded<WriteTask>(new BoundedChannelOptions(16)
                {
                    SingleReader = true,
                    SingleWriter = true
                });

            _statePool = new StreamPool(setting, UpStreamChannel);
        }
        public H2StreamSetting Setting => _setting;
        
        private void ProcessIncomingSettingFrame(SettingFrame settingFrame)
        {
            switch (settingFrame.SettingIdentifier)
            {
                case SettingIdentifier.SettingsEnablePush:
                    if (settingFrame.Value > 0)
                    {
                        // TODO Close connection on error. Push not supported 
                        return; 
                    }
                    _setting.Remote.EnablePush = false; 
                    return;
                case SettingIdentifier.SettingsMaxConcurrentStreams:
                    _setting.Remote.SettingsMaxConcurrentStreams = settingFrame.Value;
                    return;

                case SettingIdentifier.SettingsInitialWindowSize:
                    _setting.Remote.WindowSize = settingFrame.Value;
                    return;

                case SettingIdentifier.SettingsMaxFrameSize:
                    _setting.Remote.MaxFrameSize = settingFrame.Value;
                    return;

                case SettingIdentifier.SettingsMaxHeaderListSize: 
                    _setting.Remote.MaxHeaderListSize = settingFrame.Value;
                    return;

                case SettingIdentifier.SettingsHeaderTableSize:
                    _setting.SettingsHeaderTableSize = settingFrame.Value;
                    return;
            }
        }

        private async Task RaiseExceptionIfSettingNotReceived()
        {
            await Task.Delay(_setting.WaitForSettingDelay);

            if (!_waitForSettingReception.Task.IsCompleted)
                _waitForSettingReception.SetException(new H2Exception($"Server settings was not received under {(int) _setting.WaitForSettingDelay.TotalMilliseconds}."));
        }

        private async Task Init()
        {
            await _baseStream.WriteAsync(Preface, _connectionCancellationTokenSource.Token).ConfigureAwait(false);
            
            _innerReadTask = InternalReadingLoop();
            // Wait from setting reception 
            _innerWriteRun = InternalWriteLoop();
            
            var cancelTask = RaiseExceptionIfSettingNotReceived();

            await _waitForSettingReception.Task.ConfigureAwait(false);
            await cancelTask; 
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

                    _statePool.TryGetExistingActiveStream(frame.Header.StreamIdentifier, out var activeStream);

                    if (frame.Payload is SettingFrame settingFrame)
                    {
                        if (settingFrame.Ack)
                        {
                            continue;
                        }

                        ProcessIncomingSettingFrame(settingFrame);
                        settingReceived = true;
                        continue;
                    }

                    if (settingReceived && !_waitForSettingReception.Task.IsCompleted)
                    {
                        _waitForSettingReception.TrySetResult(true);
                    }

                    if (frame.Payload is IPriorityFrame priorityFrame && priorityFrame.StreamDependency > 0)
                    {
                        if (activeStream == null)
                        {
                            continue;
                        }

                        activeStream.SetPriority(priorityFrame.Exclusive, priorityFrame.StreamDependency, priorityFrame.Weight);
                    }

                    if (frame.Payload is IHeaderHolderFrame headerHolderFrame)
                    {
                        if (activeStream == null)
                        {
                            // TODO : Notify stream error, stream already closed 
                            continue;
                        }
                        
                        activeStream.ReceiveHeaderFragmentFromConnection(
                            headerHolderFrame.Data,
                            headerHolderFrame.EndHeader);

                        continue; 
                    }

                    if (frame.Payload is DataFrame dataFrame)
                    {
                        if (activeStream == null)
                        {
                            continue;
                        }

                        await
                            activeStream.ReceiveBodyFragmentFromConnection(
                                    dataFrame.Buffer, dataFrame.EndStream)
                                .ConfigureAwait(false);

                        continue; 
                    }

                    if (frame.Payload is RstStreamFrame rstStreamFrame)
                    {
                        if (activeStream == null)
                        {
                            continue;
                        }

                        activeStream.ResetRequest(rstStreamFrame.ErrorCode);
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


}