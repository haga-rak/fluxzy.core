using System;
using System.Threading.Tasks;

namespace Fluxzy.Core
{
    /// <summary>
    /// Shall be one per proxy instance
    /// </summary>
    public class ProxyScope : IAsyncDisposable
    {
        private readonly Func<IOutOfProcessHost> _captureHostBuilder;
        private readonly Func<IOutOfProcessHost, ICaptureContext> _captureContextBuilder;
        private IOutOfProcessHost? _currentCaptureHost; 

        public ProxyScope(Func<IOutOfProcessHost> captureHostBuilder,
            Func<IOutOfProcessHost, ICaptureContext> captureContextBuilder)
        {
            _captureHostBuilder = captureHostBuilder;
            _captureContextBuilder = captureContextBuilder;
        }

        public Guid Identifier { get; } = Guid.NewGuid();

        /// <summary>
        /// No thread safe  : validate that there's no risk in thread safety 
        /// </summary>
        /// <returns></returns>
        public async Task<IOutOfProcessHost?> GetOrCreateHostedCaptureHost()
        {
            if (_currentCaptureHost == null || _currentCaptureHost.FaultedOrDisposed) {
                _currentCaptureHost = null;  

                var newHost = _captureHostBuilder();

                var res = await newHost.Start();

                if (!res) {
                    // C
                    return null;  
                }

                var captureContext = _captureContextBuilder(newHost);
                await captureContext.Start();

                _currentCaptureHost = newHost; 
            }

            return _currentCaptureHost; 
        }

        public async Task<ICaptureContext> GetOrCreateHostedCaptureContext()
        {
            var host = await GetOrCreateHostedCaptureHost();

            if (host == null) {
                throw new InvalidOperationException("Unable to create capture host");
            }

            if (host.Context == null) {
                throw new InvalidOperationException("Unable to create capture context");
            }

            return host.Context;
        }

        public ICaptureContext? CaptureContext { get; set; }

        public void Dispose()
        {
        }

        public async ValueTask DisposeAsync()
        {
            if (_currentCaptureHost != null) 
             await _currentCaptureHost.DisposeAsync();
        }
    }

    public interface IOutOfProcessHost : IAsyncDisposable
    {
        Task<bool> Start();

        object Payload { get; }

        bool FaultedOrDisposed { get;  }

        ICaptureContext?  Context { get; set; }
        
    }
}