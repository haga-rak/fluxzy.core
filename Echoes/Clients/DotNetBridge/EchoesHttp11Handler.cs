using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Echoes.H11;
using Echoes.H2;
using Echoes.H2.Encoder.Utils;

namespace Echoes.DotNetBridge
{
    public class EchoesHttp11Handler : HttpMessageHandler
    {
        private readonly IDictionary<string, Http11ConnectionPool>
            _activeConnections = new Dictionary<string, Http11ConnectionPool>();

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        private readonly Http11Parser _parser = new Http11Parser(8192, new ArrayPoolMemoryProvider<char>()); 
        
        public EchoesHttp11Handler()
        {
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
                    connection = await ConnectionBuilder.CreateH11(authority, cancellationToken);

                    _activeConnections[request.RequestUri.Authority] = connection;
                }
            }
            finally
            {
                _semaphore.Release();
            }

            var reqHttpString = request.ToHttp11String();

            var exchange = new Exchange(authority, reqHttpString.AsMemory(), _parser, "HTTP/1.1");

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