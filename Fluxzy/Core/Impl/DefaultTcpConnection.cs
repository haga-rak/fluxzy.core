// Copyright © 2022 Haga Rakotoharivelo

using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Fluxzy.Core
{
    public class DefaultTcpConnection : ITcpConnection
    {
        private readonly TcpClient _client;

        public DefaultTcpConnection()
        {
            _client = new TcpClient();
        }

        public void Dispose()
        {
            _client?.Dispose();
        }

        public Task ConnectAsync(string remoteHost, int port)
        {
            return _client.ConnectAsync(remoteHost, port);
        }

        public Task ConnectAsync(IPAddress address, int port)
        {
            return _client.ConnectAsync(address, port);
        }

        public Stream GetStream()
        {
            return _client.GetStream();
        }

        public IPEndPoint LocalEndPoint => (IPEndPoint) _client.Client.LocalEndPoint;
    }
}