// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients.H2;
using Fluxzy.Core;
using Fluxzy.Misc.ResizableBuffers;

namespace Fluxzy.Clients.DotNetBridge
{
    /// <summary>
    ///  An HttpMessageHandler that uses fluxzy internals to send requests and forces connection to be HTTP/2.
    ///  Unless you know what you are doing, you should not use this class directly instead of HttpClientHandler.
    /// </summary>
    public class FluxzyHttp2Handler : HttpMessageHandler, IAsyncDisposable
    {
        private readonly IDictionary<string, H2ConnectionPool>
            _activeConnections = new Dictionary<string, H2ConnectionPool>();

        private readonly IIdProvider _idProvider;

        private readonly SemaphoreSlim _semaphore = new(1);
        private readonly H2StreamSetting _streamSetting;

        private readonly ExchangeScope _exchangeScope = new();
        public FluxzyHttp2Handler(H2StreamSetting? streamSetting = null)
        {
            _streamSetting = streamSetting ?? new H2StreamSetting();
            _idProvider = IIdProvider.FromZero;
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var connection in _activeConnections.Values) {
                await connection.DisposeAsync();
            }
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            H2ConnectionPool connectionPool;

            try {
                await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                if (!_activeConnections.TryGetValue(request.RequestUri!.Authority, out var connection)) {
                    connection = await ConnectionBuilder.CreateH2(
                        request.RequestUri.Host,
                        request.RequestUri.Port, _streamSetting, cancellationToken);

                    _activeConnections[request.RequestUri.Authority] = connection;
                }

                connectionPool = _activeConnections[request.RequestUri.Authority];
            }
            finally {
                _semaphore.Release();
            }

            var exchange = new Exchange(_idProvider, new Authority(request.RequestUri.Host, request.RequestUri.Port,
                true), request.ToHttp11String().AsMemory(), "HTTP/2", DateTime.Now);
            
            if (request.Content != null)
                exchange.Request.Body = await request.Content.ReadAsStreamAsync(cancellationToken);

            await connectionPool.Send(exchange, null!, RsBuffer.Allocate(32 * 1024), _exchangeScope,
                cancellationToken).ConfigureAwait(false);

            return new FluxzyHttpResponseMessage(exchange);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _semaphore.Dispose();
            _exchangeScope.Dispose();
        }
    }
}
