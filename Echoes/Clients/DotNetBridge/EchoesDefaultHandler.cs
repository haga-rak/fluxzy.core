using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Echoes.DotNetBridge;
using Echoes.H11;
using Echoes.H2;
using Echoes.H2.Encoder.Utils;

namespace Echoes.Clients.DotNetBridge
{
    public class EchoesDefaultHandler : HttpMessageHandler
    {
        private readonly IDictionary<string, Http11ConnectionPool>
            _activeConnections = new Dictionary<string, Http11ConnectionPool>();

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        private readonly Http11Parser _parser = new Http11Parser(8192, new ArrayPoolMemoryProvider<char>());
        private readonly PoolBuilder _poolBuilder;

        public EchoesDefaultHandler()
        {
            _poolBuilder = new PoolBuilder(new RemoteConnectionBuilder(ITimingProvider.Default),
                ITimingProvider.Default, _parser);
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var authority = new Authority(request.RequestUri.Host, request.RequestUri.Port,
                true);

            var reqHttpString = request.ToHttp11String();

            var exchange = new Exchange(authority, reqHttpString.AsMemory(), _parser);

            var connection = await _poolBuilder.GetPool(exchange, ClientSetting.Default, cancellationToken);
            
            await connection.Send(exchange,
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