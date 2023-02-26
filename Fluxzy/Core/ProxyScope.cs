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
        private IOutOfProcessHost? _currentCaptureHost; 

        public ProxyScope(Func<IOutOfProcessHost> captureHostBuilder)
        {
            _captureHostBuilder = captureHostBuilder;
        }

        public Guid Identifier { get; } = Guid.NewGuid();

        /// <summary>
        /// No thread safe  : validate that there's no risk in thread safety 
        /// </summary>
        /// <returns></returns>
        public async Task<IOutOfProcessHost?> GetOrCreateCaptureHost()
        {
            if (_currentCaptureHost == null || _currentCaptureHost.FaultedOrDisposed) {
                _currentCaptureHost = null;  

                var newHost = _captureHostBuilder();

                var res = await newHost.Start();

                if (!res) {
                    // C
                    return null;  
                }

                _currentCaptureHost = newHost; 
            }

            return _currentCaptureHost; 
        }

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

        object Context { get; }

        bool FaultedOrDisposed { get;  }
    }
}