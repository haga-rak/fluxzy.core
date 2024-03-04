// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients.Dns;
using Fluxzy.Clients.Ssl;
using Fluxzy.Clients.Ssl.BouncyCastle;
using Fluxzy.Clients.Ssl.SChannel;
using Fluxzy.Core;
using Fluxzy.Misc.ResizableBuffers;
using Fluxzy.Writers;

namespace Fluxzy.Clients.DotNetBridge
{
    /// <summary>
    ///  An HttpMessageHandler that uses fluxzy internals to send requests.
    ///  Unless you know what you are doing, you should not use this class directly instead of HttpClientHandler.
    /// </summary>
    public class FluxzyDefaultHandler : HttpMessageHandler
    {
        private readonly IIdProvider _idProvider;
        private readonly PoolBuilder _poolBuilder;
        private readonly ProxyRuntimeSetting _runtimeSetting;

        public FluxzyDefaultHandler(
            SslProvider sslProvider,
            ITcpConnectionProvider? connectionProvider = null, RealtimeArchiveWriter? writer = null)
        {
            var provider = sslProvider == SslProvider.BouncyCastle
                ? (ISslConnectionBuilder) new BouncyCastleConnectionBuilder()
                : new SChannelConnectionBuilder();
            
            _poolBuilder = new PoolBuilder(
                new RemoteConnectionBuilder(ITimingProvider.Default, provider),
                ITimingProvider.Default, new EventOnlyArchiveWriter(), new DefaultDnsSolver());

            _idProvider = IIdProvider.FromZero;

            _runtimeSetting = ProxyRuntimeSetting.CreateDefault;

            if (connectionProvider != null)
                _runtimeSetting.TcpConnectionProvider = connectionProvider;

            if (writer != null)
                _runtimeSetting.ArchiveWriter = writer;

            _runtimeSetting.Init();
        }

        public List<SslApplicationProtocol>? Protocols { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var authority = new Authority(request.RequestUri!.Host, request.RequestUri.Port,
                true);

            var reqHttpString = request.ToHttp11String();

            var exchange = new Exchange(_idProvider, authority, reqHttpString.AsMemory(), null, DateTime.Now);

            if (Protocols != null)
                exchange.Context.SslApplicationProtocols = Protocols;

            var connection = await _poolBuilder.GetPool(exchange, _runtimeSetting, cancellationToken);

            await connection.Send(exchange, null!, RsBuffer.Allocate(32 * 1024),
                cancellationToken).ConfigureAwait(false);

            return new FluxzyHttpResponseMessage(exchange);
        }
    }
}
