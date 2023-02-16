// Copyright © 2022 Haga Rakotoharivelo

using System.Threading.Channels;
using SharpPcap;
using SharpPcap.LibPcap;

namespace Fluxzy.Interop.Pcap
{
    internal class ConnectionQueueWriter : IConnectionSubscription
    {
        private readonly ChannelReader<RawCapture> _channel;
        private readonly string _outFileName;
        private readonly Task _runningWriteTask;
        private readonly CancellationToken _token;
        private readonly CancellationTokenSource _tokenSource = new();
        private readonly TimeSpan _writeBufferTimeout = TimeSpan.FromSeconds(5);

        // private CaptureFileWriterDevice? _captureDeviceWriter;
        private bool _disposed;
        private readonly CustomCaptureWriter _writer;

        public ConnectionQueueWriter(long key, ChannelReader<RawCapture> channel, string outFileName,TimestampResolution resolution)
        {
            Key = key;
            _channel = channel;
            _outFileName = outFileName;
            
            using (var deviceWriter = new CaptureFileWriterDevice(outFileName, FileMode.Create))
            {
                // Writing file header
                deviceWriter.Open();
            }
            
            _writer = new CustomCaptureWriter(_outFileName, resolution);

            _token = _tokenSource.Token;
            _runningWriteTask = Start();
        }

        public long Key { get; }


        public async ValueTask DisposeAsync()
        {
            // dispose call throws exception that happens when
            // writing in file 

            if (_disposed)
                return;

            _disposed = true;

            await End();
        }

        private async Task Start()
        {
            try
            {
                await foreach (var capture in _channel.ReadAllAsync(CancellationToken.None))
                {
                    if (true)
                    {
                        _writer.Write(capture);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // End was called 
            }
            finally
            {
                DisposeCapture();
            }
        }

        private async ValueTask End()
        {
            if (!_tokenSource.IsCancellationRequested)
                _tokenSource.CancelAfter(_writeBufferTimeout);

            try
            {
                // Waiting for files to be processed 

                await _runningWriteTask;
            }
            finally
            {
                DisposeCapture();
                _tokenSource.Dispose();
            }
        }

        private void DisposeCapture()
        {
            _writer.Dispose();

            // File.AppendAllText(_outFileName, "sd");
        }
    }
}