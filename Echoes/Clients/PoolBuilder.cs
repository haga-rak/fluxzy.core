// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using Echoes.Core.Utils;
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
        private static readonly List<SslApplicationProtocol> AllProtocols = new()
        {
            SslApplicationProtocol.Http11,
             SslApplicationProtocol.Http2
        };

        private readonly RemoteConnectionBuilder _remoteConnectionBuilder;
        private readonly ITimingProvider _timingProvider;
        private readonly Http11Parser _http11Parser;

        private readonly IDictionary<Authority, IHttpConnectionPool> _connectionPools =
            new Dictionary<Authority, IHttpConnectionPool>();

        private readonly ConcurrentDictionary<Authority, SemaphoreSlim> _lock = new();

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
        /// <param name="clientSetting"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async ValueTask<IHttpConnectionPool>
            GetPool(
            Exchange exchange,
            ClientSetting clientSetting,
            CancellationToken cancellationToken = default)
        {
            // At this point, we'll trying the suitable pool for exchange

            IHttpConnectionPool result = null;

            var semaphore = _lock.GetOrAdd(exchange.Authority, (auth) => new SemaphoreSlim(1));

            try
            {
                await semaphore.WaitAsync(cancellationToken);

                // Looking for existing HttpPool

                lock (_connectionPools)
                    if (_connectionPools.TryGetValue(exchange.Authority, out var pool))
                        return pool;

                //  pool 
                if (clientSetting.TunneledOnly || exchange.Request.Header.IsWebSocketRequest)
                {
                    var tunneledConnectionPool = new TunnelOnlyConnectionPool(
                        exchange.Authority, _timingProvider,
                        _remoteConnectionBuilder, clientSetting);
                    lock (_connectionPools)
                        return result = _connectionPools[exchange.Authority] = tunneledConnectionPool;
                }

                if (!exchange.Authority.Secure)
                {
                    // Plain HTTP/1.1
                    var http11ConnectionPool = new Http11ConnectionPool(exchange.Authority, null,
                        _remoteConnectionBuilder, _timingProvider, clientSetting, _http11Parser);

                    exchange.HttpVersion = "HTTP/1.1";

                    lock (_connectionPools)
                        return result = _connectionPools[exchange.Authority] = http11ConnectionPool;
                }

                // HTTPS test 1.1/2
                var openingResult =
                    await _remoteConnectionBuilder.OpenConnectionToRemote(exchange.Authority, false,
                        AllProtocols, clientSetting, cancellationToken);

                if (openingResult.Type == RemoteConnectionResultType.Http11)
                {
                    var http11ConnectionPool = new Http11ConnectionPool(exchange.Authority, exchange.Connection,
                        _remoteConnectionBuilder, _timingProvider, clientSetting, _http11Parser);

                    exchange.HttpVersion = "HTTP/1.1";

                    lock (_connectionPools)
                        return result = _connectionPools[exchange.Authority] = http11ConnectionPool;
                }

                if (openingResult.Type == RemoteConnectionResultType.Http2)
                {
                    var h2ConnectionPool = new H2ConnectionPool(
                        openingResult.Connection.ReadStream,  // Read and write stream are the same after the sslhandshake
                        new H2StreamSetting(),
                        exchange.Authority, exchange.Connection, OnConnectionFaulted);

                    exchange.HttpVersion = "HTTP/2";

                    lock (_connectionPools)
                        return result = _connectionPools[exchange.Authority] = h2ConnectionPool;
                }

                throw new NotSupportedException($"Unhandled protocol type {openingResult.Type}");
            }
            finally
            {
                if (result != null)
                    await result.Init();
                

                semaphore.Release();
            }
            //return null; 
        }

        private void OnConnectionFaulted(IHttpConnectionPool h2ConnectionPool)
        {
            lock (_connectionPools)
            {
                _connectionPools.Remove(h2ConnectionPool.Authority);
            }
        }
    }
}