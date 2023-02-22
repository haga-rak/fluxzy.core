using System.IO.Pipes;
using System.Net;
using Fluxzy.Capturing.Messages;

namespace Fluxzy.Interop.Pcap
{
    public class PipedCaptureContextClient : ICaptureContext, IDisposable
    {
        private readonly NamedPipeClientStream _namedPipeClientStream;
        private readonly BinaryWriter _writer;
        private readonly BinaryReader _reader;

        private PipedCaptureContextClient(string pipeName)
        {
            _namedPipeClientStream = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous)
            {

            };
            _writer = new BinaryWriter(_namedPipeClientStream);
            _reader = new BinaryReader(_namedPipeClientStream);
        }

        public static async Task<PipedCaptureContextClient> CreateAndConnect(string pipeName)
        {
            var client = new PipedCaptureContextClient(pipeName);
            try {
                await client._namedPipeClientStream.ConnectAsync();
                return client;
            }
            catch {
                client.Dispose();
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
            _namedPipeClientStream.Dispose();
        }
    }
}