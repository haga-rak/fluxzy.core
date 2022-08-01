using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients.Common;
using Fluxzy.Clients.H2.Encoder.Utils;

namespace Fluxzy.Clients.DotNetBridge
{
    public class EchoesDefaultHandler : HttpMessageHandler
    {
        private readonly SemaphoreSlim _semaphore = new(1);
        private readonly Http11Parser _parser = new(16384);
        private readonly PoolBuilder _poolBuilder;

        public EchoesDefaultHandler()
        {
            _poolBuilder = new PoolBuilder(new RemoteConnectionBuilder(ITimingProvider.Default, new DefaultDnsSolver()),
                ITimingProvider.Default, _parser, null);
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var authority = new Authority(request.RequestUri.Host, request.RequestUri.Port,
                true);

            var reqHttpString = request.ToHttp11String();

            var exchange = new Exchange(authority, reqHttpString.AsMemory(), _parser, null, DateTime.Now);

            var connection = await _poolBuilder.GetPool(exchange, ProxyRuntimeSetting.Default, cancellationToken);
            
            await connection.Send(exchange, null, new byte[32* 1024],
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