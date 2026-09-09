// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Misc.ResizableBuffers;
using Fluxzy.Writers;
using Org.BouncyCastle.Tls;

namespace Fluxzy.Clients.H11
{
    /// <summary>
    /// A pool of proxyRuntimeSetting.ConcurrentConnection HTTP/1.1 connections with the same authority 
    /// </summary>
    public class Http11ConnectionPool : IHttpConnectionPool
    {
        private static readonly List<SslApplicationProtocol> Http11Protocols = new() { SslApplicationProtocol.Http11 };
        private readonly RealtimeArchiveWriter _archiveWriter;
        private readonly DnsResolutionResult _resolutionResult;

        private readonly Channel<Http11ProcessingState> _pendingConnections;
        
        private readonly ProxyRuntimeSetting _proxyRuntimeSetting;

        private readonly RemoteConnectionBuilder _remoteConnectionBuilder;
        private readonly ITimingProvider _timingProvider;

        // Idle teardown state (the HTTP/1.1 analogue of H2's MonitorIdleAsync).
        private readonly Action<IHttpConnectionPool>? _onConnectionFaulted;
        private readonly object _idleGate = new();
        private volatile bool _complete;
        private int _activeRequestCount;
        private DateTime _lastActivity;
        private PeriodicTimer? _idleTimer;
        private Task? _idleMonitorTask;

        // Dedicated pools serve a single downstream connection (upstream pinning), so they
        // keep a connection that carried connection-oriented auth. Shared pools must drop it.
        private readonly bool _dedicated;

        // A dedicated pool must use exactly one upstream connection. Recycling back into
        // _pendingConnections happens in an async continuation on exchange.Complete
        // (RunContinuationsAsynchronously), which races the next Send's dequeue: losing that
        // race opens a second connection and breaks the connection-bound auth handshake.
        // This lease serialises Send and is released only after the connection is recycled,
        // so the next leg always finds the same connection. Also serialises concurrent
        // (HTTP/2 downstream) exchanges, which must not fan out onto multiple sockets.
        private readonly SemaphoreSlim? _pinnedLease;

        internal Http11ConnectionPool(
            Authority authority,
            RemoteConnectionBuilder remoteConnectionBuilder,
            ITimingProvider timingProvider,
            ProxyRuntimeSetting proxyRuntimeSetting,
            RealtimeArchiveWriter archiveWriter,
            DnsResolutionResult resolutionResult,
            Action<IHttpConnectionPool>? onConnectionFaulted = null,
            bool dedicated = false)
        {
            _remoteConnectionBuilder = remoteConnectionBuilder;
            _timingProvider = timingProvider;
            _proxyRuntimeSetting = proxyRuntimeSetting;
            _archiveWriter = archiveWriter;
            _resolutionResult = resolutionResult;
            _onConnectionFaulted = onConnectionFaulted;
            _dedicated = dedicated;
            _pinnedLease = dedicated ? new SemaphoreSlim(1, 1) : null;
            Authority = authority;
            _lastActivity = timingProvider.Instant();

            _pendingConnections = Channel.CreateBounded<Http11ProcessingState>(
                    new BoundedChannelOptions(proxyRuntimeSetting.ConcurrentConnection) {
                    SingleReader = false,
                    SingleWriter = false
            });

            ITimingProvider.Default.Instant();
        }

        public Authority Authority { get; }

        /// <summary>
        ///     True once torn down (idle teardown or disposal), so <see cref="PoolBuilder"/>
        ///     stops reusing it and evicts it. Was hardcoded <c>false</c>, which left idle
        ///     HTTP/1.1 pools accumulating one per host (haga-rak/fluxzy.core#634).
        /// </summary>
        public bool Complete => _complete;

        /// <summary>Test seam: drive <see cref="TryIdleTeardown"/>'s idle branch without wall-clock waits.</summary>
        internal DateTime LastActivity {
            get { lock (_idleGate) { return _lastActivity; } }
            set { lock (_idleGate) { _lastActivity = value; } }
        }

        public void Init()
        {
            // Standalone pools (e.g. ConnectionBuilder.CreateH11) pass no callback and keep
            // their previous never-torn-down behaviour.
            if (_onConnectionFaulted == null)
                return;

            var tickSeconds = Math.Clamp(_proxyRuntimeSetting.TimeOutSecondsUnusedConnection, 1, 30);
            _idleTimer = new PeriodicTimer(TimeSpan.FromSeconds(tickSeconds));
            _idleMonitorTask = MonitorIdleAsync(_idleTimer);
        }

        private async Task MonitorIdleAsync(PeriodicTimer timer)
        {
            try {
                while (await timer.WaitForNextTickAsync().ConfigureAwait(false)) {
                    if (TryIdleTeardown())
                        return;
                }
            }
            catch (Exception) {
                // Never propagate from the background loop (cf. haga-rak/fluxzy.core#614).
            }
        }

        /// <summary>
        ///     Tears down the pool when it is idle (no in-flight request, untouched for
        ///     <c>TimeOutSecondsUnusedConnection</c>): disposes pooled connections and fires
        ///     the eviction callback. Returns <c>true</c> when the monitor should stop.
        /// </summary>
        internal bool TryIdleTeardown()
        {
            List<Connection>? idleConnections = null;

            lock (_idleGate) {
                if (_complete)
                    return true;

                // Gated against Send's increment so a request that just started cannot have
                // its pool evicted from under it.
                if (_activeRequestCount != 0)
                    return false;

                if (_timingProvider.Instant() - _lastActivity
                    <= TimeSpan.FromSeconds(_proxyRuntimeSetting.TimeOutSecondsUnusedConnection))
                    return false;

                _complete = true;

                // Completing the writer makes a late OnExchangeCompleteFunction recycle
                // (TryWrite) fail, so that connection is freed instead of re-leaking.
                _pendingConnections.Writer.TryComplete();

                while (_pendingConnections.Reader.TryRead(out var state))
                    (idleConnections ??= new List<Connection>()).Add(state.Connection);
            }

            if (idleConnections != null) {
                foreach (var connection in idleConnections)
                    FreeConnectionStreams(connection);
            }

            // Fire-and-forget by contract, so this can't deadlock against DisposeAsync
            // awaiting the monitor task.
            _onConnectionFaulted?.Invoke(this);

            return true;
        }

        public async ValueTask Send(
            Exchange exchange, IDownStreamPipe downstreamPipe, RsBuffer buffer, ExchangeScope exchangeScope,
            CancellationToken cancellationToken)
        {
            ITimingProvider.Default.Instant();

            exchange.HttpVersion = "HTTP/1.1";

            lock (_idleGate) {
                if (_complete)
                    // Evicted while idle between PoolBuilder handing it out and this Send.
                    // Retryable so the orchestrator re-resolves a fresh pool (cf. the
                    // "Relaunch" path below).
                    throw new ConnectionCloseException(
                        "HTTP/1.1 connection pool was evicted after going idle; retry on a fresh pool");

                _activeRequestCount++;
                _lastActivity = _timingProvider.Instant();
            }

            var leaseAcquired = false;
            var leaseHandedToContinuation = false;
            var activityHandedToContinuation = false;

            try {
                if (_pinnedLease != null) {
                    await _pinnedLease.WaitAsync(cancellationToken).ConfigureAwait(false);
                    leaseAcquired = true;

                    // Disposal can race a waiter that entered Send before the pool was
                    // completed. Do not let that waiter open a new connection on an evicted
                    // pinned pool after the preceding exchange releases the lease.
                    lock (_idleGate) {
                        if (_complete)
                            throw new ConnectionCloseException(
                                "HTTP/1.1 connection pool was disposed while waiting for its pinned lease");
                    }
                }

                var requestDate = _timingProvider.Instant();

                // May still be true from a previous relaunched attempt: it must reflect
                // the connection actually used by this attempt, otherwise a fresh
                // connection dying before any response byte relaunches unboundedly.
                exchange.RecycledConnection = false;

                var recycledConnection = DequeueReusableConnection(requestDate);

                if (recycledConnection != null) {
                    exchange.Connection = recycledConnection;
                    exchange.RecycledConnection = true;
                }

                if (exchange.Connection == null) {
                    var openingResult =
                        await _remoteConnectionBuilder.OpenConnectionToRemote(
                            exchange, _resolutionResult , Http11Protocols,
                            _proxyRuntimeSetting, exchange.Context.ProxyConfiguration, cancellationToken);

                    if (exchange.Context.PreMadeResponse != null) {
                        return; 
                    }

                    exchange.Connection = openingResult.Connection;

                    openingResult.Connection.HttpVersion = exchange.HttpVersion;

                    if (_archiveWriter != null!)
                        _archiveWriter.Update(exchange.Connection, cancellationToken);
                }

                var poolProcessing = new Http11PoolProcessing(
                    _proxyRuntimeSetting.ExpectContinueTimeout,
                    _proxyRuntimeSetting.ResponseHeaderTimeout,
                    _proxyRuntimeSetting.ResponseBodyIdleTimeout,
                    _proxyRuntimeSetting.GetLogger<Http11PoolProcessing>());

                // Covers the whole exchange (request write, header read, body streaming):
                // closing the transport is the only reliable unblock, the TLS stack may
                // not observe the token once parked on a read. Disposed when the exchange
                // completes, before the connection can be recycled.
                var abortRegistration = cancellationToken.UnsafeRegister(
                    static state => ((Connection) state!).AbortTransport(), exchange.Connection);

                try {
                    await poolProcessing.Process(exchange, buffer, exchangeScope, cancellationToken)
                                        .ConfigureAwait(false);

                    if (exchange.Response.Header != null)
                        exchange.Connection.TimeoutIdleSeconds = exchange.Response.Header.TimeoutIdleSeconds;

                    var connection = exchange.Connection!;
                    var maxConnection = exchange.Response.Header!.MaxConnection;
                    var closeForConnectionAuth =
                        !_dedicated && ConnectionAuthHeuristic.InvolvesConnectionAuth(exchange);

                    exchange.RegisterConnectionDisposition(DisposeConnectionAfterExchange(
                        exchange, connection, maxConnection, closeForConnectionAuth,
                        abortRegistration, cancellationToken));
                    activityHandedToContinuation = true;
                    leaseHandedToContinuation = leaseAcquired;
                }
                catch (Exception ex) {

                    abortRegistration.Dispose();

                    // Any exception escaping Process leaves an HTTP/1.1 connection with an
                    // outstanding request: it is unusable and must be torn down. This
                    // includes cancellation and timeout, which would otherwise orphan a
                    // connection whose transport was already aborted.
                    var deadConnSignal = ex is ConnectionCloseException
                        || ex is TlsFatalAlert
                        || ex is IOException
                        || ex is SocketException
                        || ex is OperationCanceledException
                        || ex is ClientErrorException;

                    if (deadConnSignal) {
                        exchange.Connection?.HeaderTimeoutCts?.Dispose();

                        if (exchange.Connection?.ReadStream != null) {
                            try {
                                await exchange.Connection.ReadStream.DisposeAsync();
                            }
                            catch {
                                // transport may already be aborted
                            }
                        }

                        exchange.Connection = null;
                    }

                    // Recycled connection that died before producing any response byte —
                    // safe to relaunch on a fresh connection regardless of whether the
                    // failure happened on the request write or the response read. The
                    // recycled-and-no-response gate keeps a fresh-connection failure
                    // (server closes immediately) flowing through as 528. Cancellation
                    // and timeout (ClientErrorException) never relaunch.
                    if ((ex is TlsFatalAlert || ex is IOException || ex is SocketException)
                        && exchange.RecycledConnection
                        && exchange.Metrics.ResponseHeaderStart == default) {
                        throw new ConnectionCloseException("Relaunch");
                    }

                    throw;
                }
            }
            finally {
                if (!activityHandedToContinuation) {
                    lock (_idleGate) {
                        _activeRequestCount--;
                        _lastActivity = _timingProvider.Instant();
                    }
                }

                // Every exit that did not hand the lease to the completion continuation
                // (early return, or an exception including the Relaunch retry) releases here,
                // so a relaunch attempt does not deadlock against its own held lease.
                if (leaseAcquired && !leaseHandedToContinuation)
                    _pinnedLease!.Release();

                ITimingProvider.Default.Instant();
            }
        }

        private async Task DisposeConnectionAfterExchange(
            Exchange exchange, Connection connection, int maxConnection,
            bool closeForConnectionAuth,
            CancellationTokenRegistration abortRegistration,
            CancellationToken cancellationToken)
        {
            var closeConnection = true;
            var completionSucceeded = false;

            try {
                try {
                    closeConnection = await exchange.Complete.ConfigureAwait(false);
                    completionSucceeded = true;
                }
                catch {
                    if (exchange.Complete.Exception != null) {
                        foreach (var exception in exchange.Complete.Exception.InnerExceptions) {
                            exchange.Errors.Add(new Error("Error while reading response", exception));
                        }
                    }
                }

                try {
                    abortRegistration.Dispose();
                }
                catch (Exception exception) {
                    exchange.Errors.Add(new Error("Error while releasing response cancellation", exception));
                    closeConnection = true;
                }

                if (cancellationToken.IsCancellationRequested || closeForConnectionAuth)
                    closeConnection = true;

                if (maxConnection != -1 && maxConnection <= connection.RequestProcessed)
                    closeConnection = true;

                if (exchange.Metrics.ResponseBodyEnd == default)
                    exchange.Metrics.ResponseBodyEnd = ITimingProvider.Default.Instant();

                if (completionSucceeded && !closeConnection &&
                    _pendingConnections.Writer.TryWrite(
                        new Http11ProcessingState(connection, _timingProvider.Instant()))) {
                    return;
                }

                try {
                    FreeConnectionStreams(connection);
                }
                catch (Exception exception) {
                    exchange.Errors.Add(new Error("Error while disposing remote connection", exception));
                }
            }
            finally {
                // The request remains active until its connection is reusable or closed.
                lock (_idleGate) {
                    _activeRequestCount--;
                    _lastActivity = _timingProvider.Instant();
                }

                // A pinned sender can proceed only after disposition is complete.
                _pinnedLease?.Release();
            }
        }

        /// <summary>
        ///     Pops the next reusable pooled connection, releasing expired ones on the way.
        ///     Expired connections used to be dropped without disposal, leaking one socket,
        ///     its streams and its timeout CTS per expiry (haga-rak/fluxzy.core#744).
        /// </summary>
        internal Connection? DequeueReusableConnection(DateTime instantNow)
        {
            while (_pendingConnections.Reader.TryRead(out var state)) {
                if (HasConnectionExpired(instantNow, state)) {
                    FreeConnectionStreams(state.Connection);

                    continue;
                }

                return state.Connection;
            }

            return null;
        }

        private static void FreeConnectionStreams(Connection connection)
        {
            connection.HeaderTimeoutCts?.Dispose();
            connection.ReadStream?.Dispose();

            if (connection.ReadStream != connection.WriteStream)
                connection.WriteStream?.Dispose();
        }

        private bool HasConnectionExpired(DateTime instantNow, Http11ProcessingState state)
        {
            if (state.Connection.TimeoutIdleSeconds != -1) {

                var expireOn = state.LastUsed
                                    .AddSeconds(state.Connection.TimeoutIdleSeconds)
                                    .AddMilliseconds(-200); // 100ms to skip RTT error

                if (expireOn < instantNow)
                    return true;
            }

            var res = 
                instantNow - state.LastUsed > TimeSpan.FromSeconds(_proxyRuntimeSetting.TimeOutSecondsUnusedConnection);


            return res; 
        }

        public async ValueTask DisposeAsync()
        {
            List<Connection>? idleConnections = null;

            // Idempotent with TryIdleTeardown: whichever runs first sets _complete and
            // drains the channel; the other finds it empty/completed.
            lock (_idleGate) {
                _complete = true;
                _pendingConnections.Writer.TryComplete();

                while (_pendingConnections.Reader.TryRead(out var state))
                    (idleConnections ??= new List<Connection>()).Add(state.Connection);
            }

            if (idleConnections != null) {
                foreach (var connection in idleConnections)
                    FreeConnectionStreams(connection);
            }

            _idleTimer?.Dispose();

            // Do not dispose _pinnedLease here. DisposeAsync can race the asynchronously
            // scheduled exchange-completion continuation after a downstream connection ends.
            // The pool is already marked complete, so no new Send can acquire it; leaving this
            // small managed semaphore for GC lets the owner continuation release it safely.

            if (_idleMonitorTask != null) {
                try {
                    await _idleMonitorTask.ConfigureAwait(false);
                }
                catch {
                    // Monitor already swallows; belt-and-braces.
                }
            }
        }

        public void Dispose()
        {
        }
    }

    public class Http11ProcessingState
    {
        public Http11ProcessingState(Connection connection, DateTime lastUsed)
        {
            Connection = connection;
            LastUsed = lastUsed;
        }

        public Connection Connection { get; }

        public DateTime LastUsed { get; set; }
    }
}

