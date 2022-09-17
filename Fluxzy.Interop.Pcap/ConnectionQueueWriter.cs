// Copyright © 2022 Haga Rakotoharivelo

using System.Threading.Channels;
using SharpPcap;
using SharpPcap.LibPcap;

namespace Fluxzy.Interop.Pcap
{
    internal class ConnectionQueueWriter : IConnectionSubscription
    {
        private readonly TimeSpan _writeBufferTimeout = TimeSpan.FromSeconds(5);
        private readonly ChannelReader<RawCapture> _channel;
        private readonly string _outFileName;
        private readonly CaptureFileWriterDevice _captureDeviceWriter;
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();
        private readonly CancellationToken _token;
        private readonly Task _runningWriteTask;
        private bool _disposed = false; 

        public ConnectionQueueWriter(long key, ChannelReader<RawCapture> channel, string outFileName)
        {
            Key = key;
            _channel = channel;
            _outFileName = outFileName;
            _captureDeviceWriter = new CaptureFileWriterDevice(outFileName, System.IO.FileMode.Create);
            _captureDeviceWriter.Open();
            _token = _tokenSource.Token;
            _runningWriteTask = Start(); 
        }

        public long Key { get; }

        private async Task Start()
        {
            try
            {
                await foreach (var capture in _channel.ReadAllAsync(_token))
                {
                    if (!_token.IsCancellationRequested)
                        _captureDeviceWriter.Write(capture);
                }
            }
            catch (OperationCanceledException)
            {
                // End was called 
            }
        }

        private async ValueTask End()
        {
            if (!_tokenSource.IsCancellationRequested)
            {
                _tokenSource.CancelAfter(_writeBufferTimeout);
            }

            try
            {
                // Waiting for files to be processed 

                await _runningWriteTask;
            }
            finally
            {
                _captureDeviceWriter.StopCapture();
                _captureDeviceWriter.Dispose();
                _tokenSource.Dispose();

            }

        }
        

        public async ValueTask DisposeAsync()
        {
            // dispose call throws exception that happens when
            // writing in file 

            if (_disposed)
            {
                return;
            }

            _disposed = true; 

            await End();
        }
    }
}