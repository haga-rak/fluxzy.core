using System;
using System.Threading.Tasks;

namespace Fluxzy.Core
{
    /// <summary>
    /// Shall be one per proxy instance
    /// </summary>
    public class ProxyScope
    {
        private readonly Func<ICaptureHost> _captureHostBuilder;
        private ICaptureHost? _currentCaptureHost; 

        public ProxyScope(Func<ICaptureHost> captureHostBuilder)
        {
            _captureHostBuilder = captureHostBuilder;
        }

        public Guid Identifier { get; } = Guid.NewGuid();

        /// <summary>
        /// No thread safe  : validate that there's no risk in thread safety 
        /// </summary>
        /// <returns></returns>
        public async Task<ICaptureHost?> GetOrCreateCaptureHost()
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
    }

    public interface ICaptureHost
    {
        Task<bool> Start();

        object Context { get; }

        bool FaultedOrDisposed { get;  }
    }
}