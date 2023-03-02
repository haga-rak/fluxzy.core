using System.Net;
using System.Net.Sockets;
using Fluxzy.Core;
using Fluxzy.Interop.Pcap.Messages;

namespace Fluxzy.Interop.Pcap
{
    public class OutOfProcessCaptureContext : ICaptureContext, IDisposable
    {
        private readonly int _port;
        private readonly IOutOfProcessHost _captureHost;

        private readonly TcpClient _client;

        private BinaryWriter? _writer;
        private BinaryReader? _reader;

        public OutOfProcessCaptureContext(IOutOfProcessHost captureHost)
        {
            _port = (int)captureHost.Payload; ;
            _captureHost = captureHost;
            _client = new TcpClient();
        }
        
        public async Task Start()
        {
            await _client.ConnectAsync(IPAddress.Loopback, _port);

            var stream = _client.GetStream();
            _writer = new BinaryWriter(stream);
            _reader = new BinaryReader(stream);
            _captureHost.Context = this; 
        }

        public void Include(IPAddress remoteAddress, int remotePort)
        {
            if (_writer == null)
                return; 

            var includeMessage = new IncludeMessage(remoteAddress, remotePort);

            lock (this)
            {
                _writer.Write((byte) MessageType.Include);
                includeMessage.Write(_writer);
                _writer.Flush();
            }
        }

        public long Subscribe(string outFileName, IPAddress remoteAddress, int remotePort, int localPort)
        {
            if (_writer == null)
                return default;

            lock (this) {
                var subscribeMessage = new SubscribeMessage(remoteAddress, remotePort, localPort, outFileName);
                _writer.Write((byte)MessageType.Subscribe);
                subscribeMessage.Write(_writer);
                _writer.Flush();
                return _reader.ReadInt64();
            }

        }

        public void StoreKey(string nssKey, IPAddress remoteAddress, int remotePort, int localPort)
        {
            if (_writer == null)
                return;

            lock (this)
            {
                var storeKeyMessage = new StoreKeyMessage(remoteAddress, remotePort, localPort, nssKey);
                _writer.Write((byte) MessageType.StoreKey);
                
                storeKeyMessage.Write(_writer);
                _writer.Flush();
            }
        }

        public void ClearAll()
        {
            if (_writer == null)
                return;
            lock (this)
                _writer.Write((byte) MessageType.ClearAll);
        }

        public void Flush()
        {
            if (_writer == null)
                return;
            lock (this)
                _writer.Write((byte) MessageType.Flush);
        }

        public ValueTask Unsubscribe(long subscription)
        {
            if (_writer == null)
                return default;

            lock (this) {
                var unsubscribeMessage = new UnsubscribeMessage(subscription);
                _writer.Write((byte)MessageType.Unsubscribe);
                unsubscribeMessage.Write(_writer);
            }

            return default;
        }

        public void Dispose()
        {
            // This is a singleton, so we never dispose 

            return; 

            lock (this)
                _writer?.Write((byte)MessageType.Exit);
            _client?.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return default; 
        }
    }
}