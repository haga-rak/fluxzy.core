using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Echoes.Core
{
    internal class TunneledConnectionManager : IDisposable
    {
        private readonly IReferenceClock _referenceClock;
        private readonly Func<HttpExchange, Task> _exchangeListener;
        private readonly Func<string, Stream> _bandwidthPolicy;
        private List<TunneledConnection> _tunneledConnections = new List<TunneledConnection>();

        private bool disposed = false; 

        public TunneledConnectionManager(IReferenceClock referenceClock, Func<HttpExchange, Task> exchangeListener, Func<string, Stream> bandwidthPolicy)
        {
            _referenceClock = referenceClock;
            _exchangeListener = exchangeListener;
            _bandwidthPolicy = bandwidthPolicy;
        }

        internal void CreateTunnel(IDownStreamConnection down, IUpstreamConnection up, bool webSocket)
        {
            AddTunneledConnection(new TunneledConnection(down, up, _bandwidthPolicy(down.TargetHostName), _referenceClock, webSocket ? null : _exchangeListener));
        }

        private void AddTunneledConnection(TunneledConnection connection)
        {
            if (disposed)
                return; 

            // This is one thread proxy so no need to lock
            _tunneledConnections.Add(connection);
        }

        public void Dispose()
        {
            disposed = true; 

            if (_tunneledConnections == null)
                return; 

            foreach (var connection in _tunneledConnections.ToList())
            {
                connection.Dispose();
            }

            _tunneledConnections = null; 
        }
    }
}