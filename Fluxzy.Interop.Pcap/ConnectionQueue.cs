// Copyright © 2022 Haga Rakotoharivelo

using System.Threading.Channels;
using SharpPcap;

namespace Fluxzy.Interop.Pcap
{
    internal class ConnectionQueue : IAsyncDisposable
    {
        private readonly TimestampResolution _resolution;
        private static readonly int LimitPacketNoSubscribe = 10; 
        private readonly Channel<RawCapture> _captureChannel = Channel.CreateUnbounded<RawCapture>();
        private int _totalPackedReceived;
        private volatile bool _subscribed;
        private ConnectionQueueWriter? _writer = null;
        private bool _disposed; 

        public long Key { get; }

        public ConnectionQueue(long key, TimestampResolution resolution)
        {
            _resolution = resolution;
            Key = key;
        }

        public IConnectionSubscription Subscribe(string outFileName)
        {
            if (_writer != null)
                throw new InvalidOperationException($"Already subscribed");

            _subscribed = true;

            return _writer = new ConnectionQueueWriter(Key, _captureChannel.Reader, outFileName, _resolution);
        }

        public bool Post(RawCapture rawCapture)
        {
            Interlocked.Increment(ref _totalPackedReceived);

            if (!_subscribed && _totalPackedReceived > LimitPacketNoSubscribe)
                return false; // No subscriber on packet we leave
            
            return _captureChannel.Writer.TryWrite(rawCapture); 
        }
        
        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;

            _disposed = true;

            var res = _captureChannel.Writer.TryComplete();

            if (_writer != null)
            {
                await _writer.DisposeAsync();
          
                _writer = null;
            }
        }
    }
}