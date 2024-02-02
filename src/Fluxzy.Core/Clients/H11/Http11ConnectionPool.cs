// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Misc.ResizableBuffers;
using Fluxzy.Writers;

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

        private readonly H1Logger _logger;

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

            _logger = new H1Logger(authority);

            ITimingProvider.Default.Instant();
        }

        public Authority Authority { get; }

        public bool Complete => false;

        public void Init()
        {
        }

        public ValueTask<bool> CheckAlive()
        {
            return new ValueTask<bool>(true);
        }

        public async ValueTask Send(
            Exchange exchange, ILocalLink _, RsBuffer buffer,
            CancellationToken cancellationToken)
        {
            ITimingProvider.Default.Instant();

            exchange.HttpVersion = "HTTP/1.1";

            try {
                _logger.Trace(exchange, "Begin wait for authority slot");
                
                _logger.Trace(exchange.Id, "Acquiring slot");

                var requestDate = _timingProvider.Instant();

                while (_pendingConnections.Reader.TryRead(out var state)) {

                    if (HasConnectionExpired(requestDate, state))
                    {
                        // The connection pool exceeds timing connection ..
                        //  TODO: Gracefully release connection

                        continue;
                    }

                    exchange.Connection = state.Connection;
                    _logger.Trace(exchange.Id, () => $"Recycling connection : {exchange.Connection.Id}");

                    break;
                }

                if (exchange.Connection == null) {
                    _logger.Trace(exchange.Id, () => "New connection request");

                    var openingResult =
                        await _remoteConnectionBuilder.OpenConnectionToRemote(
                            exchange, _resolutionResult , Http11Protocols,
                            _proxyRuntimeSetting, cancellationToken);

                    if (exchange.Context.PreMadeResponse != null) {
                        return; 
                    }

                    exchange.Connection = openingResult.Connection;

                    openingResult.Connection.HttpVersion = exchange.HttpVersion;

                    if (_archiveWriter != null)
                        _archiveWriter.Update(exchange.Connection, cancellationToken);

                    _logger.Trace(exchange.Id, () => $"New connection obtained: {exchange.Connection.Id}");
                }

                var poolProcessing = new Http11PoolProcessing(_logger);

                try {
                    await poolProcessing.Process(exchange, buffer, cancellationToken)
                                        ;

                    if (exchange.Response.Header != null)
                        exchange.Connection.TimeoutIdleSeconds = exchange.Response.Header.TimeoutIdleSeconds; 

                    _logger.Trace(exchange.Id, () => "[Process] return");

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
                            _logger.Trace(exchange.Id, () => $"Complete on error {completeTask.Exception.GetType()} : {completeTask.Exception.Message}");

                            foreach (var exception in completeTask.Exception.InnerExceptions) {
                                exchange.Errors.Add(new Error("Error while reading response", exception));
                            }
                        }
                        else if (completeTask.IsCompletedSuccessfully && !closeConnectionRequest) { // 

                            if (_pendingConnections.Writer.TryWrite(new Http11ProcessingState(exchange.Connection, lastUsed)))
                            {
                                _logger.Trace(exchange.Id, () => "Complete on success, recycling connection ...");
                                return;
                            }
                        }
                        else {
                            _logger.Trace(exchange.Id, () => "Complete on success, closing connection ...");

                            // should close connection 
                        }
                        
                        FreeConnectionStreams(exchange.Connection);
                    }

                    var res = exchange.Complete.ContinueWith(OnExchangeCompleteFunction, cancellationToken);
                }
                catch (Exception ex) {

                    if (ex is ConnectionCloseException)
                    {
                        if (exchange.Connection.ReadStream != null)
                            await exchange.Connection.ReadStream.DisposeAsync();

                        exchange.Connection = null; 
                    }

                    _logger.Trace(exchange.Id, () => $"Processing error {ex}");

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
