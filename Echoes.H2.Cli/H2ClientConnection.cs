using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Echoes.H2.Cli
{
    public class H2ClientConnection : IAsyncDisposable
    { 
        private static readonly byte[] Preface = Encoding.ASCII.GetBytes("PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n");

        private readonly Stream _baseStream;
        private readonly H2ConnectionSetting _connectionSetting;

        private readonly IH2FrameReader _streamReader;
        private readonly IH2StreamWriter _streamWriter;

        private readonly CancellationTokenSource _connectionCancellationTokenSource = new CancellationTokenSource();
        private readonly H2StreamSetting _setting;

        private readonly StreamPool _statePool;

        private Task _innerReadTask;
        private Task _innerWriteRun;

        private readonly Channel<Memory<byte>> _writerChannel;

        private readonly SemaphoreSlim _writeSemaphore = new SemaphoreSlim(1); 
        

        private H2ClientConnection(
            Stream baseStream,
            H2ConnectionSetting connectionSetting,
            H2StreamSetting setting,
            IH2FrameReader streamReader,
            IH2StreamWriter streamWriter
            )
        {
            _baseStream = baseStream;
            _connectionSetting = connectionSetting;
            _streamReader = streamReader;
            _streamWriter = streamWriter;
            _setting = setting;

            _writerChannel =
                Channel.CreateBounded<Memory<byte>>(new BoundedChannelOptions(16)
                {
                    SingleReader = true,
                    SingleWriter = true
                });
            
            _statePool = new StreamPool(setting, UpStreamChannel);
        }
        public IH2StreamSetting Setting => _setting;

        private async ValueTask UpStreamChannel(Memory<byte> data, CancellationToken token)
        {
            try
            {
                // This semaphore slim is slow as it per request 
                await _writeSemaphore.WaitAsync(token).ConfigureAwait(false);
                await _baseStream.WriteAsync(data, _connectionCancellationTokenSource.Token).ConfigureAwait(false);
            }
            finally
            {
                _writeSemaphore.Release(); 
            }
        }
        
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

        private async Task Init()
        {
            await _baseStream.WriteAsync(Preface, _connectionCancellationTokenSource.Token).ConfigureAwait(false);
            _innerReadTask = InternalConnectionReadingLoop();

            // Wait from setting reception 

            _innerWriteRun = InternalWriteRun();
        }

        private async Task InternalWriteRun()
        {
            try
            {
                await foreach (var buffer in _writerChannel.Reader.ReadAllAsync(_connectionCancellationTokenSource.Token)
                    .ConfigureAwait(false))
                {
                    await _baseStream.WriteAsync(buffer).ConfigureAwait(false);
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
        private async Task InternalConnectionReadingLoop()
        {
            byte[] readBuffer = new byte[_connectionSetting.ReadBuffer];

            while (!_connectionCancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    var frame = await _streamReader.ReadNextFrameAsync(_baseStream, readBuffer,
                        _connectionCancellationTokenSource.Token).ConfigureAwait(false);

                    _statePool.TryGetExistingActiveStream(frame.Header.StreamIdentifier, out var activeStream); 

                    if (frame.Payload is IPriorityFrame priorityFrame && priorityFrame.StreamDependency > 0)
                    {
                        if (activeStream == null)
                        {
                            continue;
                        }

                        activeStream.SetPriority(priorityFrame.Exclusive, priorityFrame.StreamDependency, priorityFrame.Weight);
                    }

                    if (frame.Payload is SettingFrame settingFrame)
                    {
                        if (settingFrame.Ack)
                        {
                            continue;
                        }

                        ProcessIncomingSettingFrame(settingFrame);
                        continue;
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
            Memory<byte> requestHeader, 
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


        public static async Task<H2ClientConnection> Open(Stream stream, H2ConnectionSetting connectionSetting, IH2StreamSetting initialSetting)
        {
            // Negociating streams 

            return null;
        }


        public async ValueTask DisposeAsync()
        {
            _connectionCancellationTokenSource.Dispose();
            _writeSemaphore.Dispose();

            await _innerReadTask.ConfigureAwait(false);
            await _innerWriteRun.ConfigureAwait(false);
        }
    }

    public class H2Stream
    {

    }


    public interface IH2StreamSetting
    {
    }


    public class PeerSetting
    {
        public uint WindowSize { get; set; } = uint.MaxValue - 1;

        public uint MaxFrameSize { get; set; } = 0x4000;

        public bool EnablePush { get; set; } = false;

        public uint MaxHeaderListSize { get; set; } = 0x4000;

        public uint SettingsMaxConcurrentStreams { get; set; } = 100;
    }

    public class H2StreamSetting : IH2StreamSetting
    {
        public H2StreamSetting()
        {

        }

        public PeerSetting Local { get; set; } = new PeerSetting();

        public PeerSetting Remote { get; set; } = new PeerSetting();

        public uint SettingsHeaderTableSize { get; set; } = 4096; 
    }

    public class H2ConnectionSetting
    {
        public int ReadBuffer { get; set; } = 0x4000;

        public int WriteBuffer { get; set; } = 0x4000;
    }

    public class StreamState
    {
        public StreamStateType StateType { get; set; }

        public int WindowSize { get; set; }
    }


}