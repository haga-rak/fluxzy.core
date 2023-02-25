// Copyright © 2022 Haga Rakotoharivelo

using Fluxzy.Core;

namespace Fluxzy.Interop.Pcap
{
    public class CapturedTcpConnectionProvider : ITcpConnectionProvider
    {
        private readonly ProxyScope _scope;
        private ICaptureContext?  _directCaptureContext;

        private CapturedTcpConnectionProvider(ProxyScope scope)
        {
            _scope = scope;
        }

        public static async Task<ITcpConnectionProvider> Create(ProxyScope scope, FluxzySetting settings)
        {
            var connectionProvider =  new CapturedTcpConnectionProvider(scope); 
            connectionProvider._directCaptureContext = settings.OutOfProcCapture ?
                await OutOfProcessCaptureContext.CreateAndConnect(scope) :
                new DirectCaptureContext();

            if (connectionProvider._directCaptureContext == null) {
                Console.WriteLine("Unable to acquire authorization for capture raw packets");
                return ITcpConnectionProvider.Default; 
            }

            return connectionProvider; 
        }
        

        public ITcpConnection Create(string dumpFileName)
        {
            return new CapturableTcpConnection(_directCaptureContext, dumpFileName);
        }

        public void Dispose()
        {
        }

        public async ValueTask DisposeAsync()
        {
            await _directCaptureContext.DisposeAsync();
        }
    }
}