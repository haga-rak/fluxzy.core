// Copyright © 2021 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Echoes.H2;
using Echoes.H2.Encoder.Utils;

namespace Echoes.DotNetBridge
{
    public class EchoesHttp2Handler : HttpMessageHandler
    {
        private readonly H2StreamSetting _streamSetting;
        private readonly IDictionary<string, H2ConnectionPool>
            _activeConnections = new Dictionary<string, H2ConnectionPool>();

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        private readonly Http11Parser _parser = new Http11Parser(16384, new ArrayPoolMemoryProvider<char>()); 
        
        public EchoesHttp2Handler(H2StreamSetting streamSetting = null)
        {
            _streamSetting = streamSetting ?? new H2StreamSetting();
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

            var exchange = new Exchange(new Authority(request.RequestUri.Host, request.RequestUri.Port,
                true), request.ToHttp11String().AsMemory(), _parser, "HTTP/2");

            if (request.Content != null)
                exchange.Request.Body = await request.Content.ReadAsStreamAsync();

            await _activeConnections[request.RequestUri.Authority].Send(exchange, null,
                cancellationToken).ConfigureAwait(false);
            
            return new EchoesHttpResponseMessage(exchange);
        }


        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _semaphore.Dispose();

            foreach (var connection in _activeConnections.Values)
            {
                connection.Dispose();
            }
        }

    }
}