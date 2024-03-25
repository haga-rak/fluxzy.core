// Copyright © 2022 Haga Rakotoharivelo

namespace Fluxzy.Core.Pcap
{
    public class CapturedTcpConnectionProvider : ITcpConnectionProvider
    {
        private readonly ProxyScope _scope;
        private readonly bool _disposeProxyScope;
        private DirectCaptureContext? _createdContext;

        private CapturedTcpConnectionProvider(ProxyScope scope, bool disposeProxyScope = false)
        {
            _scope = scope;
            _disposeProxyScope = disposeProxyScope;
        }

        public ITcpConnection Create(string dumpFileName)
        {
            return new CapturableTcpConnection(_scope, dumpFileName);
        }

        public async ValueTask DisposeAsync()
        {
            if (_createdContext != null)
                await _createdContext.DisposeAsync().ConfigureAwait(false);

            if (_disposeProxyScope)
                await _scope.DisposeAsync().ConfigureAwait(false);
        }

        public static async Task<ITcpConnectionProvider> Create(ProxyScope scope, bool outOfProcCapture)
        {
            var connectionProvider = new CapturedTcpConnectionProvider(scope);

            scope.CaptureContext =
                outOfProcCapture
                    ? await scope.GetOrCreateHostedCaptureContext().ConfigureAwait(false)
                    : connectionProvider._createdContext = new DirectCaptureContext();

            if (connectionProvider._createdContext != null)
                await connectionProvider._createdContext.Start().ConfigureAwait(false);

            return connectionProvider;
        }

        public static async Task<ITcpConnectionProvider> CreateInProcessCapture()
        {
            var scope = new ProxyScope(a => new DirectCaptureContext());

            var connectionProvider = new CapturedTcpConnectionProvider(scope, true);

            scope.CaptureContext = connectionProvider._createdContext = new DirectCaptureContext();

            if (connectionProvider._createdContext != null)
                await connectionProvider._createdContext.Start().ConfigureAwait(false);

            return connectionProvider;
        }
    }
}
