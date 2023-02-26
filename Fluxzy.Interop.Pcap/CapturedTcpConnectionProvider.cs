// Copyright © 2022 Haga Rakotoharivelo

using Fluxzy.Core;

namespace Fluxzy.Interop.Pcap
{
    public class CapturedTcpConnectionProvider : ITcpConnectionProvider
    {
        private readonly ProxyScope _scope;
        private DirectCaptureContext? _createdContext;


        private CapturedTcpConnectionProvider(ProxyScope scope)
        {
            _scope = scope;
        }

        public static async Task<ITcpConnectionProvider> Create(ProxyScope scope, FluxzySetting settings)
        {
            var connectionProvider =  new CapturedTcpConnectionProvider(scope);
            
            scope.CaptureContext =
                settings.OutOfProcCapture ? 
                    await scope.GetOrCreateHostedCaptureContext() 
                    : (connectionProvider._createdContext = new DirectCaptureContext());

            if (connectionProvider._createdContext != null) {
                await connectionProvider._createdContext.Start();
            }
            
            //if (captureContext == null) {
            //    // Console.WriteLine("Unable to acquire authorization for capture raw packets");
            //    return ITcpConnectionProvider.Default; 
            //}

            return connectionProvider; 
        }
        

        public ITcpConnection Create(string dumpFileName)
        {
            return new CapturableTcpConnection(_scope, dumpFileName);
        }

        public void Dispose()
        {

        }

        public async ValueTask DisposeAsync()
        {
            if (_createdContext != null)
                await _createdContext.DisposeAsync();
        }
    }
}