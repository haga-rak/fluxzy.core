// Copyright © 2021 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using Echoes.Clients.H2.Encoder.Utils;

namespace Echoes.Clients.H11
{
    public class Http11ConnectionPool : IHttpConnectionPool
    {
        private static readonly List<SslApplicationProtocol> Http11Protocols = new() { SslApplicationProtocol.Http11 };
        
        private readonly RemoteConnectionBuilder _remoteConnectionBuilder;
        private readonly ITimingProvider _timingProvider;
        private readonly ProxyRuntimeSetting _proxyRuntimeSetting;
        private readonly Http11Parser _parser;
        private readonly RealtimeArchiveWriter _archiveWriter;
        private readonly SemaphoreSlim _semaphoreSlim;

        private readonly Queue<Http11ProcessingState> _processingStates = new();

        internal Http11ConnectionPool(
            Authority authority,
            RemoteConnectionBuilder remoteConnectionBuilder,
            ITimingProvider timingProvider,
            ProxyRuntimeSetting proxyRuntimeSetting, 
            Http11Parser parser,
            RealtimeArchiveWriter archiveWriter)
        {
            _remoteConnectionBuilder = remoteConnectionBuilder;
            _timingProvider = timingProvider;
            _proxyRuntimeSetting = proxyRuntimeSetting;
            _parser = parser;
            _archiveWriter = archiveWriter;
            Authority = authority;
            _semaphoreSlim = new SemaphoreSlim(proxyRuntimeSetting.ConcurrentConnection);
            _logger = new H1Logger(authority);
            
            ITimingProvider.Default.Instant();
        }

        public Authority Authority { get; }

        public bool Complete => false;

        public Task Init()
        {
            return Task.CompletedTask; 
        }

        public Task<bool> CheckAlive()
        {
            return Task.FromResult(true); 
        }

        private readonly H1Logger _logger;

        public async ValueTask Send(Exchange exchange, ILocalLink _, byte [] buffer, CancellationToken cancellationToken)
        {
            ITimingProvider.Default.Instant();

            exchange.HttpVersion = "HTTP/1.1";

            try
            {
                _logger.Trace(exchange, "Begin wait for authority slot");

                await _semaphoreSlim.WaitAsync(cancellationToken).ConfigureAwait(false);

                _logger.Trace(exchange.Id, "Acquiring slot");

                lock (_processingStates)
                {
                    DateTime requestDate = _timingProvider.Instant(); 

                    while (_processingStates.TryDequeue(out var state))
                    {
                        if ((requestDate - state.LastUsed) > TimeSpan.FromSeconds(_proxyRuntimeSetting.TimeOutSecondsUnusedConnection))
                        {
                            // The connection pool exceeds timing connection ..

                            continue; 
                        }

                        exchange.Connection = state.Connection;
                        _logger.Trace(exchange.Id, () => $"Recycling connection : {exchange.Connection.Id}");

                        break;
                    }
                }
                
                if (exchange.Connection == null)
                {
                    _logger.Trace(exchange.Id, () => $"New connection request");

                    var openingResult = 
                        await _remoteConnectionBuilder.OpenConnectionToRemote(exchange.Authority, false, Http11Protocols,
                        _proxyRuntimeSetting, cancellationToken);
                    
                    exchange.Connection = openingResult.Connection;

                    openingResult.Connection.HttpVersion = exchange.HttpVersion;

                    if (_archiveWriter != null)
                        await _archiveWriter.Update(exchange.Connection, cancellationToken);

                    _logger.Trace(exchange.Id, () => $"New connection obtained: {exchange.Connection.Id}");
                }

                var poolProcessing = new Http11PoolProcessing(_parser, _logger);

                try
                {
                    await poolProcessing.Process(exchange, buffer, cancellationToken)
                        .ConfigureAwait(false);


                    _logger.Trace(exchange.Id, () => $"[Process] return");

                    var res = exchange.Complete
                        .ContinueWith(completeTask =>
                        {
                            if (completeTask.Exception != null && completeTask.Exception.InnerExceptions.Any())
                            {
                                _logger.Trace(exchange.Id, () => $"Complete on error {completeTask.Exception.GetType()} : {completeTask.Exception.Message}");

                                foreach (var exception in completeTask.Exception.InnerExceptions)
                                {
                                    exchange.Errors.Add(new Error("Error while reading resp", exception));
                                }
                            }
                            else if (completeTask.IsCompletedSuccessfully && !completeTask.Result)
                            {
                                lock (_processingStates)
                                    _processingStates.Enqueue(new Http11ProcessingState(exchange.Connection, _timingProvider));

                                _logger.Trace(exchange.Id, () => $"Complete on success, recycling connection ...");

                                return; 
                            }
                            else
                            {
                                _logger.Trace(exchange.Id, () => $"Complete on success, closing connection ...");
                                // should close connection 
                            }

                            exchange.Connection.ReadStream.Dispose();

                            if (exchange.Connection.ReadStream != exchange.Connection.WriteStream)
                                exchange.Connection.WriteStream.Dispose();

                        }, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.Trace(exchange.Id, () => $"Processing error {ex}");
                    throw;
                }
            }
            finally
            {
                _semaphoreSlim.Release(); 
                ITimingProvider.Default.Instant();
            }


        }

        public async ValueTask DisposeAsync()
        {
        }

        public void Dispose()
        {
        }

    }


    public class Http11ProcessingState
    {
        private readonly Stream _stream;
        private readonly ITimingProvider _timingProvider;

        public Http11ProcessingState(Connection connection, ITimingProvider timingProvider)
        {
            Connection = connection;
            _timingProvider = timingProvider;
            LastUsed = _timingProvider.Instant();
        }

        public Connection Connection { get; }

        public DateTime LastUsed { get; set;  }
    }
}