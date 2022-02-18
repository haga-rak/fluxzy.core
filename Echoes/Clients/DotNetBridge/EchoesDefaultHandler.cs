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
        private readonly SemaphoreSlim _semaphore = new(1);
        private readonly Http11Parser _parser = new(16384, ArrayPoolMemoryProvider<char>.Default);
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

            var exchange = new Exchange(authority, reqHttpString.AsMemory(), _parser, null);

            var connection = await _poolBuilder.GetPool(exchange, ClientSetting.Default, cancellationToken);
            
            await connection.Send(exchange, null,
                cancellationToken).ConfigureAwait(false);
            
            return new EchoesHttpResponseMessage(exchange);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _semaphore.Dispose();
        }

    }
}