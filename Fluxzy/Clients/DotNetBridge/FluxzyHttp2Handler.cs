// Copyright © 2021 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients.H2;
using Fluxzy.Clients.H2.Encoder.Utils;
using Fluxzy.Misc.ResizableBuffers;

namespace Fluxzy.Clients.DotNetBridge
{
    public class FluxzyHttp2Handler : HttpMessageHandler, IAsyncDisposable
    {
        private readonly H2StreamSetting _streamSetting;
        private readonly IDictionary<string, H2ConnectionPool>
            _activeConnections = new Dictionary<string, H2ConnectionPool>();

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        private readonly Http11Parser _parser = new Http11Parser();


        private readonly IIdProvider _idProvider;

        public FluxzyHttp2Handler(H2StreamSetting streamSetting = null)
        {
            _streamSetting = streamSetting ?? new H2StreamSetting();
            _idProvider = IIdProvider.FromZero;

        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                if (!_activeConnections.TryGetValue(request.RequestUri.Authority, out var connection))
                {
                    connection = await ConnectionBuilder.CreateH2(
                        request.RequestUri.Host,
                        request.RequestUri.Port, _streamSetting, cancellationToken).ConfigureAwait(false);

                    _activeConnections[request.RequestUri.Authority] = connection;
                }
            }
            finally
            {
                _semaphore.Release();
            }

            var exchange = new Exchange(_idProvider, new Authority(request.RequestUri.Host, request.RequestUri.Port,
                true), request.ToHttp11String().AsMemory(), _parser, "HTTP/2", DateTime.Now);

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
            {
               // connection.Dispose();
            }
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var connection in _activeConnections.Values)
            {
                await connection.DisposeAsync();
            }
        }
    }
}