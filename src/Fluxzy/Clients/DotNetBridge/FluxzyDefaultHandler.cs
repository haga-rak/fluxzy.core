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
    public class FluxzyDefaultHandler : HttpMessageHandler
    {
        private readonly ITcpConnectionProvider? _connectionProvider;
        private readonly IIdProvider _idProvider;
        private readonly PoolBuilder _poolBuilder;
        private readonly ProxyRuntimeSetting _runtimeSetting;
        private readonly SemaphoreSlim _semaphore = new(1);

        public FluxzyDefaultHandler(
            SslProvider sslProvider,
            ITcpConnectionProvider? connectionProvider = null, RealtimeArchiveWriter? writer = null)
        {
            _connectionProvider = connectionProvider;

            var provider = sslProvider == SslProvider.BouncyCastle
                ? (ISslConnectionBuilder) new BouncyCastleConnectionBuilder()
                : new SChannelConnectionBuilder();

            _poolBuilder = new PoolBuilder(
                new RemoteConnectionBuilder(ITimingProvider.Default, new DefaultDnsSolver(), provider),
                ITimingProvider.Default, new EventOnlyArchiveWriter());

            _idProvider = IIdProvider.FromZero;

            _runtimeSetting = ProxyRuntimeSetting.Default;

            if (connectionProvider != null)
                _runtimeSetting.TcpConnectionProvider = connectionProvider;

            if (writer != null)
                _runtimeSetting.ArchiveWriter = writer;
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

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _semaphore.Dispose();
        }
    }
}
