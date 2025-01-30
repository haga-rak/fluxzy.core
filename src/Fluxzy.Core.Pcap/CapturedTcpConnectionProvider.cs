// Copyright © 2022 Haga Rakotoharivelo

namespace Fluxzy.Core.Pcap
{
    public class CapturedTcpConnectionProvider : ITcpConnectionProvider
    {
        private readonly IAsyncDisposable? _dependentReference;
        private readonly DirectCaptureContext? _ownedCaptureContext;
        private readonly ICaptureContext _activeContext;

        private CapturedTcpConnectionProvider(ICaptureContext? providedContext, IAsyncDisposable? dependentReference)
        {
            _dependentReference = dependentReference;
            _activeContext = providedContext ?? (_ownedCaptureContext = new DirectCaptureContext());
        }

        private async ValueTask Init()
        {
            if (_ownedCaptureContext != null)
                await _ownedCaptureContext.Start().ConfigureAwait(false);
        }

        public ITcpConnection Create(string dumpFileName)
        {
            return new CapturableTcpConnection(_activeContext, dumpFileName);
        }

        public void TryFlush()
        {
            _activeContext.Flush();
        }

        public async ValueTask DisposeAsync()
        {
            if (_ownedCaptureContext != null)
                await _ownedCaptureContext.DisposeAsync().ConfigureAwait(false);

            if (_dependentReference != null)
                await _dependentReference.DisposeAsync().ConfigureAwait(false);
        }

        public static async Task<ITcpConnectionProvider> Create(ProxyScope scope, bool outOfProcCapture)
        {
            if (!outOfProcCapture) {
                return await CreateInProcessCapture();
            }

            var captureContext = await scope.GetOrCreateHostedCaptureContext().ConfigureAwait(false);
            var connectionProvider = new CapturedTcpConnectionProvider(captureContext, null);
            await connectionProvider.Init();
            return connectionProvider;
        }

        public static async Task<ITcpConnectionProvider> CreateInProcessCapture()
        {
            var scope = new ProxyScope(a => new DirectCaptureContext());
            var connectionProvider = new CapturedTcpConnectionProvider(null, scope);
            await connectionProvider.Init();
            return connectionProvider;
        }
    }
}
