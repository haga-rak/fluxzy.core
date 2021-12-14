// Copyright © 2021 Haga Rakotoharivelo

using System.Collections.Generic;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Echoes.H2
{
    public static class H2ConnectionBuilder
    {
        public static async Task<H2ConnectionPool> Create(
            string hostName, 
            int port = 443,
            H2StreamSetting setting = default,
            CancellationToken token = default)
        {
            var tcpClient = new TcpClient();

            tcpClient.ReceiveBufferSize = 1024 * 16;

            await tcpClient.ConnectAsync(hostName, port).ConfigureAwait(false);

            var sslStream = new SslStream(tcpClient.GetStream());
            
            var sslAuthenticationOption = new SslClientAuthenticationOptions()
            {
                TargetHost = hostName,
                ApplicationProtocols = new List<SslApplicationProtocol>()
                {
                    SslApplicationProtocol.Http2,
                }
            };

            await sslStream.AuthenticateAsClientAsync(sslAuthenticationOption,
                token).ConfigureAwait(false);

            return await H2ConnectionPool.Open(sslStream, setting ?? new H2StreamSetting());
        }
    }
}