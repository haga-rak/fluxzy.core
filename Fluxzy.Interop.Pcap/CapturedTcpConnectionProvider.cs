// Copyright © 2022 Haga Rakotoharivelo

using Fluxzy.Core;

namespace Fluxzy.Interop.Pcap
{
    public class CapturedTcpConnectionProvider : ITcpConnectionProvider
    {
        private readonly ProxyScope _scope;

        public ICaptureContext? CaptureContext { get; private set; }

        private CapturedTcpConnectionProvider(ProxyScope scope)
        {
            _scope = scope;
        }

        public static async Task<ITcpConnectionProvider> Create(ProxyScope scope, FluxzySetting settings)
        {
            var connectionProvider =  new CapturedTcpConnectionProvider(scope); 
            connectionProvider.CaptureContext = settings.OutOfProcCapture ?
                await OutOfProcessCaptureContext.CreateAndConnect(scope) :
                new DirectCaptureContext();
            
            connectionProvider.CaptureContext!.Start();

            if (connectionProvider.CaptureContext == null) {
                // Console.WriteLine("Unable to acquire authorization for capture raw packets");
                return ITcpConnectionProvider.Default; 
            }

            return connectionProvider; 
        }
        

        public ITcpConnection Create(string dumpFileName)
        {
            return new CapturableTcpConnection(CaptureContext, dumpFileName);
        }

        public void Dispose()
        {

        }

        public async ValueTask DisposeAsync()
        {
            if (CaptureContext != null)
                await CaptureContext.DisposeAsync();
        }
    }
}