// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients.Dns;
using Fluxzy.Clients.H11;
using Fluxzy.Clients.H2;
using Fluxzy.Clients.Mock;
using Fluxzy.Core;
using Fluxzy.Logging;
using Fluxzy.Misc;
using Fluxzy.Rules;
using Fluxzy.Utils;
using Fluxzy.Writers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fluxzy.Clients
{
    /// <summary>
    ///     Main entry of remote connection
    /// </summary>
    internal class PoolBuilder
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

        // Upstream connections pinned to a single downstream connection (connection-oriented
        // auth). Keyed by the downstream pipe so the pins die with the client that owns them.
        private readonly ConditionalWeakTable<IDownStreamPipe, PinnedPoolSet> _pinnedPools = new();

        private readonly RemoteConnectionBuilder _remoteConnectionBuilder;
        private readonly ITimingProvider _timingProvider;
        private readonly ILogger<PoolBuilder> _logger;

        private readonly ConcurrentDictionary<string, DefaultDnsResolver> _dnsSolversCache = new();

        // Self-cleaning mode: the per-authority LockInfo/SemaphoreSlim is removed as soon
        // as no creation is in flight, otherwise the registry grows with each distinct host.
        private readonly Synchronizer<Authority> _synchronizer = new();

        public PoolBuilder(
            RemoteConnectionBuilder remoteConnectionBuilder,
            ITimingProvider timingProvider,
            RealtimeArchiveWriter archiveWriter,
            IDnsSolver dnsSolver,
            ILoggerFactory? loggerFactory = null)
        {
            _remoteConnectionBuilder = remoteConnectionBuilder;
            _timingProvider = timingProvider;
            _archiveWriter = archiveWriter;
            _dnsSolver = dnsSolver;
            _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<PoolBuilder>();
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
                IDownStreamPipe? downStreamPipe = null,
                CancellationToken cancellationToken = default)
        {
            var pool = await GetPoolCore(exchange, proxyRuntimeSetting, downStreamPipe, _logger, cancellationToken)
                .ConfigureAwait(false);
            FluxzyLogEvents.LogConnectionPoolResolved(_logger, exchange, pool);
            return pool;
        }

        private async ValueTask<IHttpConnectionPool>
            GetPoolCore(
                Exchange exchange,
                ProxyRuntimeSetting proxyRuntimeSetting,
                IDownStreamPipe? downStreamPipe,
                ILogger<PoolBuilder> logger,
                CancellationToken cancellationToken)
        {
            var dnsSolver = ResolveDnsProvider(exchange, proxyRuntimeSetting);

            var computeDnsPromise =
                DnsUtility.ComputeDnsUpdateExchange(exchange, _timingProvider, dnsSolver, proxyRuntimeSetting, logger);

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

            // Connection-oriented auth: serve the exchange from a pool dedicated to this
            // downstream connection. The pin is sticky, so the requests that follow the
            // handshake (which carry no Authorization header) keep the same connection.
            if (downStreamPipe != null) {
                var pinnedPool = GetPinnedPool(exchange, proxyRuntimeSetting, downStreamPipe, dnsResolutionResult);

                if (pinnedPool != null) {
                    if (exchange.Metrics.RetrievingPool == default)
                        exchange.Metrics.RetrievingPool = ITimingProvider.Default.Instant();

                    return pinnedPool;
                }
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
            using var syncGuard = await _synchronizer.LockAsync(exchange.Authority, cancellationToken)
                                                     .ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

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
                    _archiveWriter!, dnsResolutionResult, OnConnectionFaulted);

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
                    dnsResolutionResult, OnConnectionFaulted);

                exchange.HttpVersion = exchange.Connection!.HttpVersion = "HTTP/1.1";

                _archiveWriter.Update(openingResult.Connection, cancellationToken);

                http11ConnectionPool.Init();
                _connectionPools[exchange.Authority] = http11ConnectionPool;

                return http11ConnectionPool;
            }

            if (openingResult.Type == RemoteConnectionResultType.Http2) {
                var h2StreamSetting = exchange.Context.AdvancedTlsSettings.H2StreamSetting ?? new H2StreamSetting();

                h2StreamSetting.ResponseHeaderTimeout = proxyRuntimeSetting.ResponseHeaderTimeout;
                h2StreamSetting.ResponseBodyIdleTimeout = proxyRuntimeSetting.ResponseBodyIdleTimeout;

                var h2ConnectionPool = new H2ConnectionPool(
                    openingResult.Connection
                                 .ReadStream!, // Read and write stream are the same after the sslhandshake
                    h2StreamSetting,
                    exchange.Authority, exchange.Connection!, OnConnectionFaulted,
                    proxyRuntimeSetting.GetLogger<H2ConnectionPool>());

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

        /// <summary>
        ///     Returns the pool pinned to <paramref name="downStreamPipe"/> for this authority when
        ///     the exchange requires pinning or a pin already exists (stickiness), otherwise null.
        ///     A pinned pool is a dedicated HTTP/1.1 pool: it opens H11-only ALPN and is never
        ///     stored in the shared registry, so no other client can reuse its connection.
        /// </summary>
        private IHttpConnectionPool? GetPinnedPool(
            Exchange exchange,
            ProxyRuntimeSetting proxyRuntimeSetting,
            IDownStreamPipe downStreamPipe,
            DnsResolutionResult dnsResolutionResult)
        {
            if (!exchange.Context.RequireUpstreamPinning) {
                // Stickiness: only the follow-up requests of an already pinned authority stay
                // pinned. A pipe that never pinned anything allocates no set.
                if (!_pinnedPools.TryGetValue(downStreamPipe, out var existing)
                    || !existing.Pools.ContainsKey(exchange.Authority))
                    return null;
            }

            var pinSet = _pinnedPools.GetValue(downStreamPipe, static _ => new PinnedPoolSet());

            var pinnedPool = pinSet.Pools.GetOrAdd(exchange.Authority, authority => {
                var pool = new Http11ConnectionPool(authority,
                    _remoteConnectionBuilder, _timingProvider, proxyRuntimeSetting, _archiveWriter!,
                    dnsResolutionResult, onConnectionFaulted: null, dedicated: true);

                pool.Init();

                return pool;
            });

            exchange.HttpVersion = "HTTP/1.1";

            return pinnedPool;
        }

        /// <summary>
        ///     Disposes every pool pinned to a downstream connection. Called when that
        ///     connection ends so the dedicated upstream sockets close with it.
        /// </summary>
        public void ReleasePinnedPools(IDownStreamPipe? downStreamPipe)
        {
            if (downStreamPipe == null)
                return;

            if (!_pinnedPools.TryGetValue(downStreamPipe, out var pinSet))
                return;

            _pinnedPools.Remove(downStreamPipe);

            foreach (var pool in pinSet.Pools.Values)
                _ = ObserveDisposal(pool);
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
            var removed = _connectionPools.TryRemove(h2ConnectionPool.Authority, out _);

            if (!removed)
                return;

            FluxzyLogEvents.LogConnectionEvicted(_logger, h2ConnectionPool, "PoolFaulted");

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

        private sealed class PinnedPoolSet
        {
            public ConcurrentDictionary<Authority, IHttpConnectionPool> Pools { get; } = new();
        }
    }
}
