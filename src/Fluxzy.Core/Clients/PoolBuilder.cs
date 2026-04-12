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

        private readonly ConcurrentDictionary<Authority, IHttpConnectionPool> _connectionPools = new();
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

                    await CheckAllPoolsOnceAsync().ConfigureAwait(false);
                }
            }
            catch (TaskCanceledException) {
                // Disposed was called
            }
        }

        /// <summary>
        ///     Performs one health-check pass over the currently registered pools.
        ///     Extracted from <see cref="CheckPoolStatus"/> so tests can drive a single
        ///     tick deterministically.
        ///
        ///     A failure in one pool's <c>CheckAlive</c> must NEVER propagate out of
        ///     this method: the caller is the async-void <see cref="CheckPoolStatus"/>
        ///     loop, and an unhandled exception there terminates the process
        ///     (haga-rak/fluxzy.core#614). Each pool is checked in a try/catch and the
        ///     sweep continues regardless of individual failures.
        /// </summary>
        internal async Task CheckAllPoolsOnceAsync()
        {
            var activePools = _connectionPools.Values.ToList();

            // Sequential: CheckAlive does no network I/O on either the alive or the
            // idle-teardown branch, so concurrency would only add coordination cost.
            // Per-pool try/catch ensures one bad pool can't poison the sweep.
            for (var i = 0; i < activePools.Count; i++) {
                try {
                    await activePools[i].CheckAlive().ConfigureAwait(false);
                }
                catch (Exception) {
                    // Swallow: if a pool's health check throws, the next sweep will
                    // observe Complete==true (assuming the failure put it in that
                    // state) and reap it via the GetPool fast path. Logging hook
                    // can be added once the project has a structured logger here.
                }
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

            var forceNewConnection = exchange.Context.ForceNewConnection;

            if (exchange.Request.Header.IsWebSocketRequest || exchange.Context.BlindMode)
                forceNewConnection = true;

            // Fast path: reuse existing pool without acquiring the per-authority semaphore.
            // Pools are only stored after Init(), so any pool found here is fully initialised.
            if (!forceNewConnection
                && _connectionPools.TryGetValue(exchange.Authority, out var existingPool)
                && !existingPool.Complete) {

                if (exchange.Metrics.RetrievingPool == default)
                    exchange.Metrics.RetrievingPool = ITimingProvider.Default.Instant();

                exchange.Metrics.ReusingConnection = true;

                return existingPool;
            }

            // Slow path: acquire per-authority semaphore for pool creation
            using var syncGuard = await _synchronizer.LockAsync(exchange.Authority);

            // Double-check after acquiring semaphore — another thread may have
            // created the pool while we waited.
            if (!forceNewConnection) {
                if (_connectionPools.TryGetValue(exchange.Authority, out var pool)) {
                    if (pool.Complete) {
                        _connectionPools.TryRemove(exchange.Authority, out _);
                    }
                    else {
                        if (exchange.Metrics.RetrievingPool == default)
                            exchange.Metrics.RetrievingPool = ITimingProvider.Default.Instant();

                        exchange.Metrics.ReusingConnection = true;

                        return pool;
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

                tunneledConnectionPool.Init();

                return tunneledConnectionPool;
            }

            if (exchange.Request.Header.IsWebSocketRequest) {
                var tunneledConnectionPool = new WebsocketConnectionPool(
                    exchange.Authority, _timingProvider,
                    _remoteConnectionBuilder, proxyRuntimeSetting, dnsResolutionResult);

                tunneledConnectionPool.Init();

                return tunneledConnectionPool;
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

                http11ConnectionPool.Init();
                _connectionPools[exchange.Authority] = http11ConnectionPool;

                return http11ConnectionPool;
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

                http11ConnectionPool.Init();
                _connectionPools[exchange.Authority] = http11ConnectionPool;

                return http11ConnectionPool;
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

                try {
                    h2ConnectionPool.Init();
                }
                catch {
                    _ = ObserveDisposal(h2ConnectionPool);
                    throw;
                }

                _connectionPools[exchange.Authority] = h2ConnectionPool;

                return h2ConnectionPool;
            }

            throw new NotSupportedException($"Unhandled protocol type {openingResult.Type}");
        }

        private IDnsSolver ResolveDnsProvider(Exchange exchange, ProxyRuntimeSetting proxyRuntimeSetting)
        {
            return string.IsNullOrWhiteSpace(exchange.Context.DnsOverHttpsNameOrUrl) ? 
                _dnsSolver : _dnsSolversCache.GetOrAdd(exchange.Context.DnsOverHttpsNameOrUrl,
                    n => new DnsOverHttpsResolver(n, exchange.Context.DnsOverHttpsCapture ?
                        proxyRuntimeSetting.GetInternalProxyAuthentication() : null));
        }

        /// <summary>
        ///     Test seam: directly inserts a pool into the active pool dictionary so
        ///     <see cref="CheckAllPoolsOnceAsync"/> picks it up. Avoids constructing
        ///     the full <see cref="GetPool"/> collaborator stack.
        /// </summary>
        internal void TryAddPoolForTests(Authority authority, IHttpConnectionPool pool)
        {
            _connectionPools[authority] = pool;
        }

        private void OnConnectionFaulted(IHttpConnectionPool h2ConnectionPool)
        {
            var removed = _connectionPools.TryRemove(h2ConnectionPool.Authority, out _);

            if (!removed)
                return;

            // Disposal runs asynchronously after the H2ConnectionPool's own
            // OnLoopEnd cleanup has completed (see the reordering in
            // H2ConnectionPool.OnLoopEnd). We do not block the caller here, but we
            // also do not discard the ValueTask: ObserveDisposal awaits it and
            // swallows any error so it doesn't surface as UnobservedTaskException.
            _ = ObserveDisposal(h2ConnectionPool);
        }

        private static async Task ObserveDisposal(IHttpConnectionPool pool)
        {
            try {
                await pool.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception) {
                // Swallow: a faulted disposal is not actionable from this context.
                // A structured logger hook can be added here once available.
            }
        }
    }
}
