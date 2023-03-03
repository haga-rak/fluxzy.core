using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients.H11;
using Fluxzy.Clients.H2;
using Fluxzy.Misc.ResizableBuffers;

namespace Fluxzy.Clients.DotNetBridge
{
    public class FluxzyHttp11Handler : HttpMessageHandler
    {
        private readonly SslProvider _sslProvider;

        private readonly IDictionary<string, Http11ConnectionPool>
            _activeConnections = new Dictionary<string, Http11ConnectionPool>();

        private readonly SemaphoreSlim _semaphore = new(1);
        private readonly IIdProvider _idProvider;

        public FluxzyHttp11Handler(SslProvider sslProvider = SslProvider.OsDefault)
        {
            _sslProvider = sslProvider;
            _idProvider = IIdProvider.FromZero;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var authority = new Authority(request.RequestUri.Host, request.RequestUri.Port,
                true);

            try
            {
                await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                if (!_activeConnections.TryGetValue(request.RequestUri.Authority, out var connection))
                {
                    connection = await ConnectionBuilder.CreateH11(authority, _sslProvider, cancellationToken);

                    _activeConnections[request.RequestUri.Authority] = connection;
                }
            }
            finally
            {
                _semaphore.Release();
            }

            var reqHttpString = request.ToHttp11String();

            var exchange = new Exchange(_idProvider, authority, reqHttpString.AsMemory(), "HTTP/1.1", DateTime.Now);

            if (request.Content != null)
                exchange.Request.Body = await request.Content.ReadAsStreamAsync();

            await _activeConnections[request.RequestUri.Authority].Send(exchange, null, RsBuffer.Allocate(32 * 1024),
                cancellationToken).ConfigureAwait(false);

            return new FluxzyHttpResponseMessage(exchange);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _semaphore.Dispose();

            foreach (var connection in _activeConnections.Values)
                connection.Dispose();
        }
    }

    public enum SslProvider
    {
        OsDefault = 1 , 
        BouncyCastle
    }
}
