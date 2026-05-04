// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        internal Http11ConnectionPool(
            Authority authority,
            RemoteConnectionBuilder remoteConnectionBuilder,
            ITimingProvider timingProvider,
            ProxyRuntimeSetting proxyRuntimeSetting,
            RealtimeArchiveWriter archiveWriter,
            DnsResolutionResult resolutionResult)
        {
            _remoteConnectionBuilder = remoteConnectionBuilder;
            _timingProvider = timingProvider;
            _proxyRuntimeSetting = proxyRuntimeSetting;
            _archiveWriter = archiveWriter;
            _resolutionResult = resolutionResult;
            Authority = authority;

            _pendingConnections = Channel.CreateBounded<Http11ProcessingState>(
                    new BoundedChannelOptions(proxyRuntimeSetting.ConcurrentConnection) {
                    SingleReader = false,
                    SingleWriter = false
            });

            ITimingProvider.Default.Instant();
        }

        public Authority Authority { get; }

        public bool Complete => false;

        public void Init()
        {
        }

        public async ValueTask Send(
            Exchange exchange, IDownStreamPipe downstreamPipe, RsBuffer buffer, ExchangeScope exchangeScope,
            CancellationToken cancellationToken)
        {
            ITimingProvider.Default.Instant();

            exchange.HttpVersion = "HTTP/1.1";

            try {
                var requestDate = _timingProvider.Instant();

                while (_pendingConnections.Reader.TryRead(out var state)) {

                    if (HasConnectionExpired(requestDate, state))
                    {
                        // The connection pool exceeds timing connection ..
                        //  TODO: Gracefully release connection

                        continue;
                    }

                    exchange.Connection = state.Connection;
                    exchange.RecycledConnection = true;
                    break;
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
                    _proxyRuntimeSetting.GetLogger<Http11PoolProcessing>());

                try {
                    await poolProcessing.Process(exchange, buffer, exchangeScope, cancellationToken)
                                        .ConfigureAwait(false);

                    if (exchange.Response.Header != null)
                        exchange.Connection.TimeoutIdleSeconds = exchange.Response.Header.TimeoutIdleSeconds; 
                    
                    var lastUsed = _timingProvider.Instant(); 

                    void OnExchangeCompleteFunction(Task<bool> completeTask)
                    {
                        var closeConnectionRequest = completeTask.Result;

                        if (exchange.Response.Header!.MaxConnection != -1 &&
                            exchange.Response.Header!.MaxConnection <= exchange.Connection.RequestProcessed) {
                            closeConnectionRequest = true; 
                        }

                        if (exchange.Metrics.ResponseBodyEnd == default)
                            exchange.Metrics.ResponseBodyEnd = ITimingProvider.Default.Instant();

                        if (completeTask.Exception != null && completeTask.Exception.InnerExceptions.Any()) {
                            foreach (var exception in completeTask.Exception.InnerExceptions) {
                                exchange.Errors.Add(new Error("Error while reading response", exception));
                            }
                        }
                        else if (completeTask.IsCompletedSuccessfully && !closeConnectionRequest) { // 

                            if (_pendingConnections.Writer.TryWrite(
                                    new Http11ProcessingState(exchange.Connection, lastUsed)))
                            {
                                return;
                            }
                        }
                        else {
                            // should close connection 
                        }
                        
                        FreeConnectionStreams(exchange.Connection);
                    }

                    var _ = exchange.Complete.ContinueWith(OnExchangeCompleteFunction, cancellationToken);
                }
                catch (Exception ex) {

                    // Any "connection is dead" signal must dispose the read stream and
                    // null the connection so the next attempt opens a fresh one. The
                    // original code only handled ConnectionCloseException, leaking the
                    // read stream when TlsFatalAlert / IOException / SocketException
                    // bubbled through unconverted (e.g. from the request-write path).
                    if (ex is ConnectionCloseException
                        || ex is TlsFatalAlert
                        || ex is IOException
                        || ex is SocketException) {
                        if (exchange.Connection?.ReadStream != null)
                            await exchange.Connection.ReadStream.DisposeAsync();

                        exchange.Connection = null;
                    }
                    
                    throw;
                }
            }
            finally {
                //_semaphoreSlim.Release();
                ITimingProvider.Default.Instant();
            }
        }

        private static void FreeConnectionStreams(Connection connection)
        {
            connection.ReadStream!.Dispose();

            if (connection.ReadStream != connection.WriteStream)
                connection.WriteStream!.Dispose();
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

        public ValueTask DisposeAsync()
        {
            return default;
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
