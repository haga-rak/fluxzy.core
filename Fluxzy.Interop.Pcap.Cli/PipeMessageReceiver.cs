using System.Net;
using System.Net.Sockets;
using Fluxzy.Interop.Pcap.Messages;

namespace Fluxzy.Interop.Pcap.Cli
{
    public class PipeMessageReceiver
    {
        private readonly Func<SubscribeMessage, long> _subscribeHandler;
        private readonly Action<UnsubscribeMessage> _unsubscribeHandler;
        private readonly Action<IncludeMessage> _includeHandler;
        private readonly Action _flushHandler;
        private readonly CancellationToken _token;
        private readonly Task _taskLoop;
        private readonly TcpListener _tcpListener;

        public PipeMessageReceiver(
            Func<SubscribeMessage, long> subscribeHandler,
            Action<UnsubscribeMessage> unsubscribeHandler,
            Action<IncludeMessage> includeHandler,
            Action flushHandler,
            CancellationToken token)
        {
            _subscribeHandler = subscribeHandler;
            _unsubscribeHandler = unsubscribeHandler;
            _includeHandler = includeHandler;
            _flushHandler = flushHandler;
            _token = token;

            _tcpListener  = new TcpListener(IPAddress.Loopback, 0);
            _tcpListener.Start();
            ListeningPort = ((IPEndPoint)_tcpListener.LocalEndpoint).Port;
            _taskLoop = Task.Run(async () => await InternalLoop());
        }

        public int ListeningPort { get; }

        private async Task InternalLoop()
        {
            // await _pipeServer.WaitForConnectionAsync(_token);

            try {

                var client = await _tcpListener.AcceptTcpClientAsync(_token);
                var stream = client.GetStream();

                var binaryWriter = new BinaryWriter(stream);
                var binaryReader = new BinaryReader(stream);
                var @byte = new byte[1];

                while ((await stream.ReadAsync(@byte, 0, 1, _token)) > 0)
                {
                    var messageType = (MessageType)@byte[0];

                    switch (messageType)
                    {
                        case MessageType.Subscribe:
                            var subscribeMessage = SubscribeMessage.FromReader(binaryReader);
                            var key = _subscribeHandler(subscribeMessage);
                            binaryWriter.Write(key);
                            break;
                        case MessageType.Unsubscribe:
                            var unsubscribeMessage = UnsubscribeMessage.FromReader(binaryReader);
                            _unsubscribeHandler(unsubscribeMessage);
                            break;
                        case MessageType.Include:
                            var includeMessage = IncludeMessage.FromReader(binaryReader);
                            _includeHandler(includeMessage);
                            break;
                        case MessageType.Flush:
                            _flushHandler();
                            break;
                        case MessageType.Exit:
                            return;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
            finally {
                _tcpListener.Stop(); // We free the port 
            }
        }

        public async ValueTask WaitForExit()
        {
            await _taskLoop; 
        }
    }
}