// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients.Dns;
using Fluxzy.Clients.DotNetBridge;
using Fluxzy.Clients.H11;
using Fluxzy.Clients.Ssl;
using Fluxzy.Clients.Ssl.BouncyCastle;
using Fluxzy.Clients.Ssl.SChannel;
using Fluxzy.Core;

namespace Fluxzy.Clients.H2
{
    public static class ConnectionBuilder
    {
        public static async Task<H2ConnectionPool> CreateH2(
            string hostName,
            int port = 443,
            H2StreamSetting? setting = default,
            CancellationToken token = default)
        {
            var tcpClient = new TcpClient();

            tcpClient.ReceiveBufferSize = 1024 * 128;

            await tcpClient.ConnectAsync(hostName, port);

            var sslStream = new SslStream(tcpClient.GetStream());

            var sslAuthenticationOption = new SslClientAuthenticationOptions {
                TargetHost = hostName,
                ApplicationProtocols = new List<SslApplicationProtocol> {
                    SslApplicationProtocol.Http2
                }
            };

            await sslStream.AuthenticateAsClientAsync(sslAuthenticationOption,
                token);

            if (sslStream.NegotiatedApplicationProtocol != SslApplicationProtocol.Http2)
                throw new NotSupportedException($"Remote ({hostName}:{port}) cannot talk HTTP2");

            var authority = new Authority(hostName, port, true);

            var connectionPool = new H2ConnectionPool(sslStream, setting ?? new H2StreamSetting(),
                authority, new Connection(authority, IIdProvider.FromZero), _ => { });

            connectionPool.Init();

            return connectionPool;
        }

        public static async Task<Http11ConnectionPool> CreateH11(
            Authority authority, SslProvider provider,
            CancellationToken token = default)
        {
            var sslProvider = provider == SslProvider.BouncyCastle
                ? (ISslConnectionBuilder)new BouncyCastleConnectionBuilder()
                : new SChannelConnectionBuilder();

            var dnsSolver = new DefaultDnsSolver();
            var timingProvider = new ITimingProvider.DefaultTimingProvider();
            var result = await DnsUtility.ComputeDns(authority, timingProvider, dnsSolver);

            var connectionPool = new Http11ConnectionPool(authority,
                new RemoteConnectionBuilder(ITimingProvider.Default,
                    sslProvider),
                ITimingProvider.Default, ProxyRuntimeSetting.Default, null!, result);

            connectionPool.Init();

            return connectionPool;
        }
    }
}
