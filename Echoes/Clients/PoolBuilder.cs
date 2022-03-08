// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
    public class PoolBuilder : IDisposable
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
        private readonly CancellationTokenSource _poolCheckHaltSource = new(); 
        

        public PoolBuilder(
            RemoteConnectionBuilder remoteConnectionBuilder,
            ITimingProvider timingProvider,
            Http11Parser http11Parser)
        {
            _remoteConnectionBuilder = remoteConnectionBuilder;
            _timingProvider = timingProvider;
            _http11Parser = http11Parser;

            CheckPoolStatus(_poolCheckHaltSource.Token); 
        }

        private async void CheckPoolStatus(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    // TODO put delay into config files or settings
                    
                    await Task.Delay(5000, token);

                    List<IHttpConnectionPool> activePools;

                    lock (_connectionPools)
                        activePools = _connectionPools.Values.ToList();

                    await Task.WhenAll(activePools.Select(s => s.CheckAlive()));
                }
            }
            catch (TaskCanceledException)
            {
                // Disposed was called 
            }
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
                    while (_connectionPools.TryGetValue(exchange.Authority, out var pool))
                    {
                        if (pool.Complete)
                        {
                            _connectionPools.Remove(pool.Authority);
                            continue;
                        }

                        return pool;
                    }

                //  pool 
                if (clientSetting.TunneledOnly || exchange.Request.Header.IsWebSocketRequest)
                {
                    var tunneledConnectionPool = new TunnelOnlyConnectionPool(
                        exchange.Authority, _timingProvider,
                        _remoteConnectionBuilder, clientSetting);

                    return result = tunneledConnectionPool;
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
                try
                {
                    if (result != null)
                        await PoolInit(result);

                    exchange.Metrics.RetrievingPool = ITimingProvider.Default.Instant();
                }
                finally
                {
                    semaphore.Release();

                }

            }
            //return null; 
        }

        private async Task PoolInit(IHttpConnectionPool result)
        {
            try
            {
                await result.Init();
            }
            catch (Exception)
            {
                OnConnectionFaulted(result);

                throw; 
            }
        }

        private void OnConnectionFaulted(IHttpConnectionPool h2ConnectionPool)
        {
            lock (_connectionPools)
            {
                _connectionPools.Remove(h2ConnectionPool.Authority);
            }
        }

        public void Dispose()
        {
            _poolCheckHaltSource.Cancel();
        }
    }
}