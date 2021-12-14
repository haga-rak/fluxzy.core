// Copyright © 2021 Haga Rakotoharivelo

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Echoes.H2
{
    public class HttpConnectionPoolBuilder
    {
        private readonly IDictionary<Authority, IHttpConnectionPool> _connections 
            = new ConcurrentDictionary<Authority, IHttpConnectionPool>()

        public async Task<IHttpConnectionPool> GetOrCreate(
            Authority authority, 
            PendingRequest pendingRequest = null,
            bool tunnelOnly = false)
        {
            if (_connections.TryGetValue(authority, out var httpConnectionPool))
                return httpConnectionPool; 

            // Plain HTTP 

            if (tunnelOnly)
            {
                // Create the tunneled connection
            }

            if (pendingRequest != null)
            {
                // Proceed to create a connection pool with  HTTP Plain stream 
            }

            // Build TCP Client with ssl streams 


            // Unlike HTTP pool connection 

        }
    }
}