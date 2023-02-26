using System.Net;
using System.Net.Sockets;
using Fluxzy.Core;
using Fluxzy.Interop.Pcap.Messages;

namespace Fluxzy.Interop.Pcap
{
    public class OutOfProcessCaptureContext : ICaptureContext, IDisposable
    {
        private readonly int _port;
        private readonly ProxyScope _proxyScope;

        private readonly TcpClient _client;

        private BinaryWriter _writer;
        private BinaryReader _reader;

        private OutOfProcessCaptureContext(int port, ProxyScope proxyScope)
        {
            _port = port;
            _proxyScope = proxyScope;
            _client = new TcpClient(); 
        }

        public static async Task<OutOfProcessCaptureContext?> CreateAndConnect(ProxyScope proxyScope)
        {
            var captureHost = await proxyScope.GetOrCreateCaptureHost();

            if (captureHost == null)
                return null; 
            
            var port = (int) captureHost.Context; 

            var context = new OutOfProcessCaptureContext(port, proxyScope);
            try {
                await context._client.ConnectAsync(IPAddress.Loopback, port);

                var stream = context._client.GetStream();
                context._writer = new BinaryWriter(stream);
                context._reader = new BinaryReader(stream);

                return context;
            }
            catch {
                context.Dispose();
                throw;
            }
        }

        public void Start()
        {
            
        }

        public void Include(IPAddress remoteAddress, int remotePort)
        {
            var includeMessage = new IncludeMessage(remoteAddress, remotePort);
            
            _writer.Write((byte) MessageType.Include);
            includeMessage.Write(_writer);
            _writer.Flush();
        }

        public long Subscribe(string outFileName, IPAddress remoteAddress, int remotePort, int localPort)
        {
            var subscribeMessage = new SubscribeMessage(remoteAddress, remotePort, localPort, outFileName);
            _writer.Write((byte) MessageType.Subscribe);
            subscribeMessage.Write(_writer);
            _writer.Flush();
            var key = _reader.ReadInt64();

            return key; 
        }

        public ValueTask Unsubscribe(long subscription)
        {
            var unsubscribeMessage = new UnsubscribeMessage(subscription);
            _writer.Write((byte) MessageType.Unsubscribe);
            unsubscribeMessage.Write(_writer);
            return default;
        }

        public void Dispose()
        {
            _writer.Write((byte)MessageType.Exit);
            _client.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return default; 
        }
    }
}