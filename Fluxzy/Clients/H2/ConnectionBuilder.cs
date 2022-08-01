// Copyright © 2021 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Echoes.Clients.Common;
using Echoes.Clients.H11;
using Echoes.Clients.H2.Encoder.Utils;

namespace Echoes.Clients.H2
{
    public static class ConnectionBuilder
    {
        public static async Task<H2ConnectionPool> CreateH2(
            string hostName, 
            int port = 443,
            H2StreamSetting setting = default,
            CancellationToken token = default)
        {
            var tcpClient = new TcpClient();

            tcpClient.ReceiveBufferSize = 1024 * 128;

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

            if (sslStream.NegotiatedApplicationProtocol != SslApplicationProtocol.Http2)
                throw new NotSupportedException($"Remote ({hostName}:{port}) cannot talk HTTP2");

            var authority = new Authority(hostName, port, true);

            var connectionPool = new H2ConnectionPool(sslStream, setting ?? new H2StreamSetting(),
                authority, new Connection(authority), _ => {});

            await connectionPool.Init();


            return connectionPool;
        }

        public static async Task<Http11ConnectionPool> CreateH11(Authority authority, 
            CancellationToken token = default)
        {
            var connectionPool =  new Http11ConnectionPool(authority,
                new RemoteConnectionBuilder(ITimingProvider.Default, new DefaultDnsSolver()),
                ITimingProvider.Default, ProxyRuntimeSetting.Default, new Http11Parser(
                    16 * 1024), null);

            await connectionPool.Init();

            return connectionPool; 
        }
    }
}