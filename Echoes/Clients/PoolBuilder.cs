// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using Echoes.H11;
using Echoes.H2;
using Echoes.H2.Encoder.Utils;

namespace Echoes
{
    /// <summary>
    /// Main entry of remote connection
    /// </summary>
    public class PoolBuilder
    {
        private static List<SslApplicationProtocol> _allProtocols = new List<SslApplicationProtocol>()
        {
            SslApplicationProtocol.Http11, SslApplicationProtocol.Http2
        }; 

        private readonly RemoteConnectionBuilder _remoteConnectionBuilder;
        private readonly ITimingProvider _timingProvider;
        private readonly Http11Parser _http11Parser;

        private IDictionary<Authority, IHttpConnectionPool> _connectionPools =
            new Dictionary<Authority, IHttpConnectionPool>();

        public PoolBuilder(
            RemoteConnectionBuilder remoteConnectionBuilder,
            ITimingProvider timingProvider, 
            Http11Parser http11Parser)
        {
            _remoteConnectionBuilder = remoteConnectionBuilder;
            _timingProvider = timingProvider;
            _http11Parser = http11Parser;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="exchange"></param>
        /// <param name="creationSetting"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async ValueTask<IHttpConnectionPool> GetPool(
            Exchange exchange, 
            ClientSetting creationSetting, 
            CancellationToken cancellationToken = default)
        {
            // At this point, we just come to receive an exchange from the proxy client 
            // we still don't know if the remote server is HTTP/2 capable or not 

            if (_connectionPools.TryGetValue(exchange.Authority, out var pool))
                return pool; 

            //  pool 
            if (creationSetting.TunneledOnly)
            {
                var tunneledConnectionPool = new TunnelOnlyConnectionPool(exchange.Authority, _timingProvider,
                   _remoteConnectionBuilder, creationSetting);

                await tunneledConnectionPool.Init();

                return _connectionPools[exchange.Authority] = tunneledConnectionPool; 
            }

            if (!exchange.Authority.Secure)
            {
                // Plain HTTP/1.1
                var http11ConnectionPool = new Http11ConnectionPool(exchange.Authority, null,null,
                    _remoteConnectionBuilder, _timingProvider, creationSetting, _http11Parser);

                await http11ConnectionPool.Init();

                return _connectionPools[exchange.Authority] = http11ConnectionPool;
            }

            // HTTPS test 1/2

            var negotiatedProtocol = await _remoteConnectionBuilder.OpenConnectionToRemote(exchange, false,
                _allProtocols, creationSetting, cancellationToken);

            if (negotiatedProtocol == RemoteConnectionResult.Http11)
            {
                var http11ConnectionPool = new Http11ConnectionPool(exchange.Authority, exchange.Connection, exchange.UpStream,
                    _remoteConnectionBuilder, _timingProvider, creationSetting, _http11Parser);

                await http11ConnectionPool.Init();

                return _connectionPools[exchange.Authority] = http11ConnectionPool;
            }

            if (negotiatedProtocol == RemoteConnectionResult.Http2)
            {
                var h2ConnectionPool = new H2ConnectionPool(exchange.UpStream, new H2StreamSetting(),
                    exchange.Authority, exchange.Connection);

                await h2ConnectionPool.Init();

                return _connectionPools[exchange.Authority] = h2ConnectionPool;
            }

            throw new NotSupportedException($"Unhandled protocol type {negotiatedProtocol}");
        }
        
    }
}