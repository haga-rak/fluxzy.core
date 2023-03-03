// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients.H11;
using Fluxzy.Clients.H2;
using Fluxzy.Clients.Mock;
using Fluxzy.Misc;
using Fluxzy.Writers;

namespace Fluxzy.Clients
{
    /// <summary>
    ///     Main entry of remote connection
    /// </summary>
    internal class PoolBuilder : IDisposable
    {
        private static readonly List<SslApplicationProtocol> AllProtocols = new()
        {
            SslApplicationProtocol.Http11,
            SslApplicationProtocol.Http2
        };
        
        private readonly RealtimeArchiveWriter _archiveWriter;

        private readonly IDictionary<Authority, IHttpConnectionPool> _connectionPools =
            new Dictionary<Authority, IHttpConnectionPool>();

        private readonly ConcurrentDictionary<Authority, SemaphoreSlim> _lock = new();
        private readonly CancellationTokenSource _poolCheckHaltSource = new();

        private readonly RemoteConnectionBuilder _remoteConnectionBuilder;
        private readonly ITimingProvider _timingProvider;

        public PoolBuilder(
            RemoteConnectionBuilder remoteConnectionBuilder,
            ITimingProvider timingProvider,
            RealtimeArchiveWriter archiveWriter)
        {
            _remoteConnectionBuilder = remoteConnectionBuilder;
            _timingProvider = timingProvider;
            _archiveWriter = archiveWriter;

            CheckPoolStatus(_poolCheckHaltSource.Token);
        }

        public void Dispose()
        {
            _poolCheckHaltSource.Cancel();
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
                    {
                        activePools = _connectionPools.Values.ToList();
                    }

                    await ValueTaskUtil.WhenAll(activePools.Select(s => s.CheckAlive()).ToArray());
                }
            }
            catch (TaskCanceledException)
            {
                // Disposed was called 
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="exchange"></param>
        /// <param name="proxyRuntimeSetting"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async ValueTask<IHttpConnectionPool>
            GetPool(
                Exchange exchange,
                ProxyRuntimeSetting proxyRuntimeSetting,
                CancellationToken cancellationToken = default)
        {
            // At this point, we'll trying the suitable pool for exchange

            if (exchange.Context.PreMadeResponse != null)
                return new MockedConnectionPool(exchange.Authority,
                    exchange.Context.PreMadeResponse);

            IHttpConnectionPool? result = null;

            var semaphorePerAuthority = _lock.GetOrAdd(exchange.Authority, auth => new SemaphoreSlim(1));
            var released = false;

            try
            {
                if (!semaphorePerAuthority.Wait(0))
                    await semaphorePerAuthority.WaitAsync(cancellationToken);

                // Looking for existing HttpPool

                lock (_connectionPools)
                {
                    while (_connectionPools.TryGetValue(exchange.Authority, out var pool))
                    {
                        if (pool.Complete)
                        {
                            _connectionPools.Remove(pool.Authority);

                            continue;
                        }

                        if (exchange.Metrics.RetrievingPool == default)
                            exchange.Metrics.RetrievingPool = ITimingProvider.Default.Instant();

                        exchange.Metrics.ReusingConnection = true; 

                        return pool;
                    }
                }

                if (exchange.Metrics.RetrievingPool == default)
                     exchange.Metrics.RetrievingPool = ITimingProvider.Default.Instant();

                //  pool 
                if (exchange.Context.BlindMode)
                {
                    var tunneledConnectionPool = new TunnelOnlyConnectionPool(
                        exchange.Authority, _timingProvider,
                        _remoteConnectionBuilder, proxyRuntimeSetting);

                    return result = tunneledConnectionPool;
                }

                if (exchange.Request.Header.IsWebSocketRequest)
                {
                    var tunneledConnectionPool = new WebsocketConnectionPool(
                        exchange.Authority, _timingProvider,
                        _remoteConnectionBuilder, proxyRuntimeSetting);

                    return result = tunneledConnectionPool;
                }

                if (!exchange.Authority.Secure)
                {
                    // Plain HTTP/1, no h2c
                    var http11ConnectionPool = new Http11ConnectionPool(exchange.Authority,
                        _remoteConnectionBuilder, _timingProvider, proxyRuntimeSetting,
                        _archiveWriter!);

                    exchange.HttpVersion = "HTTP/1.1";

                    lock (_connectionPools)
                    {
                        return result = _connectionPools[exchange.Authority] = http11ConnectionPool;
                    }
                }

                // HTTPS test 1.1/2

                RemoteConnectionResult openingResult = default;

                try {
                    openingResult =
                        (await _remoteConnectionBuilder.OpenConnectionToRemote(
                            exchange,
                            exchange.Context.SslApplicationProtocols ?? AllProtocols, proxyRuntimeSetting,
                            cancellationToken))!;
                }
                catch {
                    if (exchange.Connection != null)
                        _archiveWriter.Update(exchange.Connection, cancellationToken);
                    throw; 
                }

                // exchange.Connection = openingResult.Connection;

                if (openingResult.Type == RemoteConnectionResultType.Http11)
                {
                    var http11ConnectionPool = new Http11ConnectionPool(exchange.Authority,
                        _remoteConnectionBuilder, _timingProvider, proxyRuntimeSetting, _archiveWriter);

                    exchange.HttpVersion = exchange.Connection.HttpVersion = "HTTP/1.1";
                    
                    _archiveWriter.Update(openingResult.Connection, cancellationToken);

                    lock (_connectionPools)
                    {
                        return result = _connectionPools[exchange.Authority] = http11ConnectionPool;
                    }
                }

                if (openingResult.Type == RemoteConnectionResultType.Http2)
                {
                    var h2ConnectionPool = new H2ConnectionPool(
                        openingResult.Connection
                                     .ReadStream!, // Read and write stream are the same after the sslhandshake
                        new H2StreamSetting(),
                        exchange.Authority, exchange.Connection, OnConnectionFaulted);

                    exchange.HttpVersion = exchange.Connection.HttpVersion = "HTTP/2";

                    if (_archiveWriter != null)
                        _archiveWriter.Update(openingResult.Connection, cancellationToken);

                    lock (_connectionPools)
                    {
                        return result = _connectionPools[exchange.Authority] = h2ConnectionPool;
                    }
                }

                throw new NotSupportedException($"Unhandled protocol type {openingResult.Type}");
            }
            finally
            {
                try
                {
                    if (result != null)
                    {
                        released = true;
                        semaphorePerAuthority.Release();

                        result.Init();
                    }

                }
                catch
                {
                    if (result != null)
                        OnConnectionFaulted(result);
                }
                finally
                {
                    if (!released)
                        semaphorePerAuthority.Release();
                }
            }
        }

        private void OnConnectionFaulted(IHttpConnectionPool h2ConnectionPool)
        {
            lock (_connectionPools)
            {
                if (_connectionPools.Remove(h2ConnectionPool.Authority))
                    h2ConnectionPool.DisposeAsync();
            }

            try
            {
                // h2ConnectionPool.Dispose();
            }
            catch
            {
                // Dispose and suppress errors
            }
        }
    }
}
