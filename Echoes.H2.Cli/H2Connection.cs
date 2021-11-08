using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Echoes.H2.Cli
{
    public class H2Connection : IAsyncDisposable
    {
        private static readonly byte[] Preface = Encoding.ASCII.GetBytes("PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n");

        private readonly Stream _baseStream;
        private readonly H2ConnectionSetting _connectionSetting;
        private readonly IH2StreamReader _streamReader;
        private readonly IH2StreamWriter _streamWriter;
        private Task _innerTask;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private Dictionary<int, StreamState> _overallState = new Dictionary<int, StreamState>();
        
        private H2Connection(
            Stream baseStream, 
            H2ConnectionSetting connectionSetting,
            H2StreamSetting setting, 
            IH2StreamReader streamReader,
            IH2StreamWriter streamWriter
            )
        {
            _baseStream = baseStream;
            _connectionSetting = connectionSetting;
            _streamReader = streamReader;
            _streamWriter = streamWriter;
            Setting = setting;
            _innerTask = InternalRun();
            _overallState[0].StateType = StreamStateType.Open;
        }

        public IH2StreamSetting Setting { get; }


        private async Task CloseRemoteConnection()
        {
            // Echoes does not support Push 
        }


        private async Task ProcessIncomingSettingFrame(SettingFrame settingFrame)
        {
            if (settingFrame.SettingIdentifier == SettingIdentifier.SettingsEnablePush)
            {
                if (settingFrame.Value > 0)
                {
                    // Close connection on error 

                }
            }

            if (settingFrame.SettingIdentifier == SettingIdentifier.SettingsMaxConcurrentStreams)
            {

            }
        }


        private async Task Init()
        {
            byte [] readBuffer = new byte[_connectionSetting.ReadBuffer];

            await _baseStream.WriteAsync(Preface, _cancellationTokenSource.Token).ConfigureAwait(false);

            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    var frame = await _streamReader.ReadNextFrameAsync(_baseStream, readBuffer,
                        _cancellationTokenSource.Token).ConfigureAwait(false);

                    if (frame.Payload is SettingFrame settingFrame)
                    {
                        if (settingFrame.Ack)
                        {
                            continue; 
                        }

                        await ProcessIncomingSettingFrame(settingFrame).ConfigureAwait(false); 
                    }
                }
                catch
                {
                    throw; 
                }
            }
        }


        private async Task InternalRun()
        {
             // Frame loop 
        }


        public async Task Write(Stream requestStream, Stream responseStream)
        {
            // Find stream channel 
            // Write data 
            // Wait data 
        }


        public static async Task<H2Connection> Open(Stream stream, H2ConnectionSetting connectionSetting, IH2StreamSetting initialSetting)
        {
            // Negociating streams 



            return null; 
        }


        public async ValueTask DisposeAsync()
        {
            _cancellationTokenSource.Dispose();
            await _innerTask.ConfigureAwait(false); 
        }
    }

    public class H2Stream
    {

    }


    public interface IH2StreamSetting
    {
        int SettingsHeaderTableSize { get; }

        bool SettingsEnablePush { get; }

        int SettingsMaxConcurrentStreams { get; }

        int SettingsInitialWindowSize { get; }

        int SettingsMaxFrameSize { get; }

        int SettingsMaxHeaderListSize { get; }
    }

    public class H2StreamSetting : IH2StreamSetting
    {
        public H2StreamSetting()
        {

        }

        public int SettingsHeaderTableSize { get; set; } = 4096; 

        public bool SettingsEnablePush { get; set; } = false; 

        public int SettingsMaxConcurrentStreams { get; set; } = 100; 

        public int SettingsInitialWindowSize { get;  set; } = 0xffff; 

        public int SettingsMaxFrameSize { get; set; } = 0x4000; 

        public int SettingsMaxHeaderListSize { get;  set; } = 0x4000;
        
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


    public enum StreamStateType : ushort
    {
        Idle = 0 ,
        ReservedLocal, 
        ReservedRemote, 
        Open, 
        CloseLocal,
        CloseRemote, 
        Closed
    }
}