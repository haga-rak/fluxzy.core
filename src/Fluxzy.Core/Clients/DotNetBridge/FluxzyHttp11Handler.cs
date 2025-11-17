// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients.H11;
using Fluxzy.Clients.H2;
using Fluxzy.Core;
using Fluxzy.Misc.ResizableBuffers;

namespace Fluxzy.Clients.DotNetBridge
{
    /// <summary>
    ///  An HttpMessageHandler that uses fluxzy internals to send requests and forces connection to be HTTP/1.1.
    ///  Unless you know what you are doing, you should not use this class directly instead of HttpClientHandler.
    /// </summary>
    public class FluxzyHttp11Handler : HttpMessageHandler
    {
        private readonly IDictionary<string, Http11ConnectionPool>
            _activeConnections = new Dictionary<string, Http11ConnectionPool>();

        private readonly IIdProvider _idProvider;

        private readonly SemaphoreSlim _semaphore = new(1);
        private readonly SslProvider _sslProvider;

        private readonly ExchangeScope _exchangeScope = new();

        public FluxzyHttp11Handler(SslProvider sslProvider = SslProvider.OsDefault)
        {
            _sslProvider = sslProvider;
            _idProvider = IIdProvider.FromZero;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var authority = new Authority(request.RequestUri!.Host, request.RequestUri.Port,
                true);

            Http11ConnectionPool connectionPool; 

            try {
                await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                if (!_activeConnections.TryGetValue(request.RequestUri.Authority, out var connection)) {
                    connection = await ConnectionBuilder.CreateH11(authority, _sslProvider, cancellationToken).ConfigureAwait(false);

                    _activeConnections[request.RequestUri.Authority] = connection;
                }

                connectionPool = _activeConnections[request.RequestUri.Authority];
            }
            finally {
                _semaphore.Release();
            }

            var reqHttpString = request.ToHttp11String();

            var exchange = new Exchange(_idProvider, authority, reqHttpString.AsMemory(), "HTTP/1.1", DateTime.Now);

            if (request.Content != null)
                exchange.Request.Body = await request.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            
            await connectionPool.Send(exchange, null!, RsBuffer.Allocate(32 * 1024), _exchangeScope,
                cancellationToken).ConfigureAwait(false);

            return new FluxzyHttpResponseMessage(exchange);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _semaphore.Dispose();

            foreach (var connection in _activeConnections.Values) {
                connection.Dispose();
            }
            _exchangeScope.Dispose();
        }
    }

    /// <summary>
    /// The ssl provider used by the handlers
    /// </summary>
    public enum SslProvider
    {
        /// <summary>
        /// The .NET default SSL provider for the OS. 
        /// </summary>
        OsDefault = 1,

        /// <summary>
        ///  The BouncyCastle SSL provider. It is a managed implementation in .NET
        /// </summary>
        BouncyCastle
    }
}
