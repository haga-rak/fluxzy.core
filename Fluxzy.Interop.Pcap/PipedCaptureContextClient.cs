using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using Fluxzy.Capturing.Messages;

namespace Fluxzy.Interop.Pcap
{
    public class PipedCaptureContextClient : ICaptureContext, IDisposable
    {
        private readonly int _port;
        private readonly TcpClient _client;

        private BinaryWriter _writer;
        private BinaryReader _reader;

        private PipedCaptureContextClient(int port)
        {
            _port = port;
            _client = new TcpClient(); 
        }

        public static async Task<PipedCaptureContextClient> CreateAndConnect(int port)
        {
            var context = new PipedCaptureContextClient(port);
            try {
                // await client._namedPipeClientStream.ConnectAsync();
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
    }
}