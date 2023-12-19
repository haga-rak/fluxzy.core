// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;

namespace Fluxzy.Tests._Fixtures
{
    internal static class ReverseProxyHelper
    {
        public static SocketsHttpHandler GetSpoofedHandler(int proxyPort, string host, bool secure = true)
        {
            var handler = new SocketsHttpHandler()
            {
                ConnectCallback = async (_, cancellationToken) =>
                {
                    var tcpConnection = new TcpClient();

                    await tcpConnection.ConnectAsync(new IPEndPoint(IPAddress.Loopback, proxyPort),
                        cancellationToken);

                    var networkStream = tcpConnection.GetStream();

                    if (secure)
                    {
                        var sslStream = new SslStream(networkStream, false);
                        await sslStream.AuthenticateAsClientAsync(host);
                        return sslStream;
                    }

                    return networkStream;
                }
            };

            handler.AllowAutoRedirect = false;

            return handler;
        }

    }
}
