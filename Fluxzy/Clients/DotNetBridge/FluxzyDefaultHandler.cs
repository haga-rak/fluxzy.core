using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients.Common;
using Fluxzy.Clients.Ssl.SChannel;
using Fluxzy.Misc.ResizableBuffers;

namespace Fluxzy.Clients.DotNetBridge
{
    public class FluxzyDefaultHandler : HttpMessageHandler
    {
        private readonly SemaphoreSlim _semaphore = new(1);
        private readonly PoolBuilder _poolBuilder;
        private readonly IIdProvider _idProvider;

        public FluxzyDefaultHandler()
        {
            _poolBuilder = new PoolBuilder(new RemoteConnectionBuilder(ITimingProvider.Default, new DefaultDnsSolver(), new SChannelConnectionBuilder()),
                ITimingProvider.Default, null);

            _idProvider = IIdProvider.FromZero;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var authority = new Authority(request.RequestUri.Host, request.RequestUri.Port,
                true);

            var reqHttpString = request.ToHttp11String();

            var exchange = new Exchange(_idProvider, authority, reqHttpString.AsMemory(), null, DateTime.Now);

            var connection = await _poolBuilder.GetPool(exchange, ProxyRuntimeSetting.Default, cancellationToken);

            await connection.Send(exchange, null, RsBuffer.Allocate(32 * 1024),
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
