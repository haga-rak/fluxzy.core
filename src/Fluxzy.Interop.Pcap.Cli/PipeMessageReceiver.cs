// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net;
using System.Net.Sockets;
using Fluxzy.Interop.Pcap.Messages;

namespace Fluxzy.Interop.Pcap.Cli
{
    public class PipeMessageReceiver
    {
        private readonly Action _clearAllHandler;
        private readonly Action _flushHandler;
        private readonly Action<IncludeMessage> _includeHandler;
        private readonly Action<StoreKeyMessage> _storeKeyHandler;
        private readonly Func<SubscribeMessage, long> _subscribeHandler;
        private readonly Task<int> _taskLoop;
        private readonly TcpListener _tcpListener;

        private readonly CancellationToken _token;
        private readonly Action<UnsubscribeMessage> _unsubscribeHandler;

        public PipeMessageReceiver(
            Func<SubscribeMessage, long> subscribeHandler,
            Action<StoreKeyMessage> storeKeyHandler,
            Action<UnsubscribeMessage> unsubscribeHandler,
            Action<IncludeMessage> includeHandler,
            Action flushHandler,
            Action clearAllHandler,
            CancellationToken token)
        {
            _subscribeHandler = subscribeHandler;
            _storeKeyHandler = storeKeyHandler;
            _unsubscribeHandler = unsubscribeHandler;
            _includeHandler = includeHandler;
            _flushHandler = flushHandler;
            _clearAllHandler = clearAllHandler;
            _token = token;

            _tcpListener = new TcpListener(IPAddress.Loopback, 0);
            _tcpListener.Start();
            ListeningPort = ((IPEndPoint) _tcpListener.LocalEndpoint).Port;
            _taskLoop = Task.Run(async () => await InternalLoop());
        }

        public int ListeningPort { get; }

        private async Task<int> InternalLoop()
        {
            try {
                using var client = await _tcpListener.AcceptTcpClientAsync(_token);

                client.NoDelay = true;

                await using var stream = client.GetStream();

                var binaryWriter = new BinaryWriter(stream);
                var binaryReader = new BinaryReader(stream);
                var @byte = new byte[1];

                while (await stream.ReadAsync(@byte, 0, 1, _token) > 0) {
                    var messageType = (MessageType) @byte[0];

                    switch (messageType) {
                        case MessageType.Subscribe:
                            var subscribeMessage = SubscribeMessage.FromReader(binaryReader);
                            var key = _subscribeHandler(subscribeMessage);
                            binaryWriter.Write(key);

                            break;

                        case MessageType.StoreKey:
                            var storeKeyMessage = StoreKeyMessage.FromReader(binaryReader);
                            _storeKeyHandler(storeKeyMessage);

                            break;

                        case MessageType.Unsubscribe:
                            var unsubscribeMessage = UnsubscribeMessage.FromReader(binaryReader);
                            _unsubscribeHandler(unsubscribeMessage);

                            break;

                        case MessageType.Include:
                            var includeMessage = IncludeMessage.FromReader(binaryReader);
                            _includeHandler(includeMessage);
                            binaryWriter.Write(0);

                            break;

                        case MessageType.ClearAll:
                            _clearAllHandler();

                            break;

                        case MessageType.Flush:
                            _flushHandler();

                            break;

                        case MessageType.Exit:
                            return 80;

                        default:
                            throw new ArgumentOutOfRangeException(); // FATAL EXIT
                    }
                }

                return 81;
            }
            catch (Exception ex) {
                // TODO set logger here 
                return 90;
            }
            finally {
                _tcpListener.Stop(); // We free the port 
            }
        }

        public async ValueTask<int> WaitForExit()
        {
            return await _taskLoop;
        }
    }
}
