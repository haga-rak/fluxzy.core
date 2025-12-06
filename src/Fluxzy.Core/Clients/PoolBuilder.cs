// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients.Dns;
using Fluxzy.Clients.H11;
using Fluxzy.Clients.H2;
using Fluxzy.Clients.Mock;
using Fluxzy.Core;
using Fluxzy.Misc;
using Fluxzy.Rules;
using Fluxzy.Utils;
using Fluxzy.Writers;

namespace Fluxzy.Clients
{
    /// <summary>
    ///     Main entry of remote connection
    /// </summary>
    internal class PoolBuilder : IDisposable
    {
        private static readonly List<SslApplicationProtocol> AllProtocols = new() {
            SslApplicationProtocol.Http2,
            SslApplicationProtocol.Http11
        };

        static PoolBuilder()
        {
            if (string.Equals(Environment.GetEnvironmentVariable("FLUXZY_DISABLE_H2")?.Trim(), "1")) {
                AllProtocols.Remove(SslApplicationProtocol.Http2); 
            }
        }

        private readonly RealtimeArchiveWriter _archiveWriter;
        private readonly IDnsSolver _dnsSolver;

        private readonly IDictionary<Authority, IHttpConnectionPool> _connectionPools =
            new Dictionary<Authority, IHttpConnectionPool>();
        private readonly CancellationTokenSource _poolCheckHaltSource = new();

        private readonly RemoteConnectionBuilder _remoteConnectionBuilder;
        private readonly ITimingProvider _timingProvider;

        private readonly ConcurrentDictionary<string, DefaultDnsResolver> _dnsSolversCache = new();

        private readonly Synchronizer<Authority> _synchronizer = new(true);

        public PoolBuilder(
            RemoteConnectionBuilder remoteConnectionBuilder,
            ITimingProvider timingProvider,
            RealtimeArchiveWriter archiveWriter,
            IDnsSolver dnsSolver)
        {
            _remoteConnectionBuilder = remoteConnectionBuilder;
            _timingProvider = timingProvider;
            _archiveWriter = archiveWriter;
            _dnsSolver = dnsSolver;
            CheckPoolStatus(_poolCheckHaltSource.Token);
        }
        
        public void Dispose()
        {
            _poolCheckHaltSource.Cancel();
        }

        private async void CheckPoolStatus(CancellationToken token)
        {
            try {
                while (!token.IsCancellationRequested) {
                    // TODO put delay into config files or settings

                    await Task.Delay(5000, token).ConfigureAwait(false);

                    List<IHttpConnectionPool> activePools;

                    lock (_connectionPools) {
                        activePools = _connectionPools.Values.ToList();
                    }

                    await ValueTaskUtil.WhenAll(activePools.Select(s => s.CheckAlive()).ToArray()).ConfigureAwait(false);
                }
            }
            catch (TaskCanceledException) {
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
            var dnsSolver = ResolveDnsProvider(exchange, proxyRuntimeSetting);

            var computeDnsPromise = 
                DnsUtility.ComputeDnsUpdateExchange(exchange, _timingProvider, dnsSolver, proxyRuntimeSetting);

            var dnsResolutionResult = await computeDnsPromise.ConfigureAwait(false);

            await proxyRuntimeSetting.EnforceRules(exchange.Context,
                FilterScope.DnsSolveDone,
                exchange.Connection, exchange).ConfigureAwait(false);

            if (exchange.Context.PreMadeResponse != null) {
                var mockedConnectionPool =
                    new MockedConnectionPool(exchange.Authority, exchange.Context.PreMadeResponse);
                mockedConnectionPool.Init();
                return mockedConnectionPool;
            }

            IHttpConnectionPool? result = null;

            try
            {
                using var _ = await _synchronizer.LockAsync(exchange.Authority);

                var forceNewConnection = exchange.Context.ForceNewConnection;

                if (exchange.Request.Header.IsWebSocketRequest || exchange.Context.BlindMode)
                    forceNewConnection = true;

                // Looking for existing HttpPool

                if (!forceNewConnection) {
                    lock (_connectionPools) 
                    {
                        if (_connectionPools.TryGetValue(exchange.Authority, out var pool)) 
                        {
                            if (pool.Complete) {
                                _connectionPools.Remove(pool.Authority);
                            }
                            else {
                                if (exchange.Metrics.RetrievingPool == default)
                                    exchange.Metrics.RetrievingPool = ITimingProvider.Default.Instant();

                                exchange.Metrics.ReusingConnection = true;

                                return pool;
                            }
                        }
                    }
                }

                if (exchange.Metrics.RetrievingPool == default)
                    exchange.Metrics.RetrievingPool = ITimingProvider.Default.Instant();


                //  pool 
                if (exchange.Context.BlindMode && exchange.Authority.Secure) {
                    var tunneledConnectionPool = new TunnelOnlyConnectionPool(
                        exchange.Authority, _timingProvider,
                        _remoteConnectionBuilder, proxyRuntimeSetting, dnsResolutionResult);

                    return result = tunneledConnectionPool;
                }

                if (exchange.Request.Header.IsWebSocketRequest) {
                    var tunneledConnectionPool = new WebsocketConnectionPool(
                        exchange.Authority, _timingProvider,
                        _remoteConnectionBuilder, proxyRuntimeSetting, dnsResolutionResult);

                    return result = tunneledConnectionPool;
                }

                if (!exchange.Authority.Secure) {
                    // Plain HTTP/1, no h2c

                    var http11ConnectionPool = new Http11ConnectionPool(exchange.Authority,
                        _remoteConnectionBuilder, _timingProvider, proxyRuntimeSetting,
                        _archiveWriter!, dnsResolutionResult);

                    exchange.HttpVersion = "HTTP/1.1";

                    if (exchange.Context.PreMadeResponse != null)
                    {
                        return new MockedConnectionPool(exchange.Authority,
                            exchange.Context.PreMadeResponse);
                    }

                    lock (_connectionPools) {
                        return result = _connectionPools[exchange.Authority] = http11ConnectionPool;
                    }
                }

                // HTTPS test 1.1/2

                RemoteConnectionResult openingResult;
                try
                {
                    openingResult =
                        (await _remoteConnectionBuilder.OpenConnectionToRemote(
                            exchange, dnsResolutionResult,
                            exchange.Context.SslApplicationProtocols ?? AllProtocols, proxyRuntimeSetting,
                            exchange.Context.ProxyConfiguration,
                            cancellationToken).ConfigureAwait(false))!;

                    if (exchange.Context.PreMadeResponse != null)
                    {
                        return new MockedConnectionPool(exchange.Authority,
                            exchange.Context.PreMadeResponse);
                    }

                }
                catch {
                    if (exchange.Connection != null)
                        _archiveWriter.Update(exchange.Connection, cancellationToken);

                    throw;
                }


                if (openingResult.Type == RemoteConnectionResultType.Http11) {
                    var http11ConnectionPool = new Http11ConnectionPool(exchange.Authority,
                        _remoteConnectionBuilder, _timingProvider, proxyRuntimeSetting, _archiveWriter,
                        dnsResolutionResult);

                    exchange.HttpVersion = exchange.Connection!.HttpVersion = "HTTP/1.1";

                    _archiveWriter.Update(openingResult.Connection, cancellationToken);

                    lock (_connectionPools) {
                        return result = _connectionPools[exchange.Authority] = http11ConnectionPool;
                    }
                }

                if (openingResult.Type == RemoteConnectionResultType.Http2) {
                    var h2ConnectionPool = new H2ConnectionPool(
                        openingResult.Connection
                                     .ReadStream!, // Read and write stream are the same after the sslhandshake
                        exchange.Context.AdvancedTlsSettings.H2StreamSetting ?? new H2StreamSetting(),
                        exchange.Authority, exchange.Connection!, OnConnectionFaulted);

                    exchange.HttpVersion = exchange.Connection!.HttpVersion = "HTTP/2";

                    if (_archiveWriter != null!)
                        _archiveWriter.Update(openingResult.Connection, cancellationToken);

                    lock (_connectionPools) {
                        return result = _connectionPools[exchange.Authority] = h2ConnectionPool;
                    }
                }

                throw new NotSupportedException($"Unhandled protocol type {openingResult.Type}");
            }
            finally {
                if (result != null) {
                    try
                    {
                        result.Init();
                    }
                    catch
                    {
                        OnConnectionFaulted(result);
                    }
                }
            }
        }

        private IDnsSolver ResolveDnsProvider(Exchange exchange, ProxyRuntimeSetting proxyRuntimeSetting)
        {
            return string.IsNullOrWhiteSpace(exchange.Context.DnsOverHttpsNameOrUrl) ? 
                _dnsSolver : _dnsSolversCache.GetOrAdd(exchange.Context.DnsOverHttpsNameOrUrl,
                    n => new DnsOverHttpsResolver(n, exchange.Context.DnsOverHttpsCapture ?
                        proxyRuntimeSetting.GetInternalProxyAuthentication() : null));
        }

        private void OnConnectionFaulted(IHttpConnectionPool h2ConnectionPool)
        {
            lock (_connectionPools) {
                if (_connectionPools.Remove(h2ConnectionPool.Authority))
                    h2ConnectionPool.DisposeAsync();
            }

            try {
                // h2ConnectionPool.Dispose();
            }
            catch {
                // Dispose and suppress errors
            }
        }
    }
}
