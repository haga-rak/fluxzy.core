// Copyright © 2021 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using Echoes.H2.Encoder.Utils;

namespace Echoes.H11
{
    public class Http11ConnectionPool : IHttpConnectionPool
    {
        private static readonly List<SslApplicationProtocol> Http11Protocols = new List<SslApplicationProtocol>() { SslApplicationProtocol.Http11 };
        
        private readonly RemoteConnectionBuilder _remoteConnectionBuilder;
        private readonly ITimingProvider _timingProvider;
        private readonly ClientSetting _clientSetting;
        private readonly Http11Parser _parser;
        private readonly SemaphoreSlim _semaphoreSlim;
        private readonly Queue<Http11ProcessingState> _processingStates = new Queue<Http11ProcessingState>();

        private DateTime _lastActivity = ITimingProvider.Default.Instant(); 

        public Http11ConnectionPool(
            Authority authority, 
            Connection ? existingConnection,
            RemoteConnectionBuilder remoteConnectionBuilder,
            ITimingProvider timingProvider,
            ClientSetting clientSetting, 
            Http11Parser parser)
        {
            _remoteConnectionBuilder = remoteConnectionBuilder;
            _timingProvider = timingProvider;
            _clientSetting = clientSetting;
            _parser = parser;
            Authority = authority;
            _semaphoreSlim = new SemaphoreSlim(clientSetting.ConcurrentConnection);
            _logger = new H1Logger(authority);

            if (existingConnection != null)
            {
                _processingStates.Enqueue(new Http11ProcessingState(existingConnection, _timingProvider));
            }

            _lastActivity = ITimingProvider.Default.Instant();
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
            _lastActivity = ITimingProvider.Default.Instant();

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
                        if ((requestDate - state.LastUsed) > TimeSpan.FromSeconds(_clientSetting.TimeOutSecondsUnusedConnection))
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
                        _clientSetting, cancellationToken);
                    
                    exchange.Connection = openingResult.Connection;

                    _logger.Trace(exchange.Id, () => $"New connection obtained: {exchange.Connection.Id}");
                }

                var poolProcessing = new Http11PoolProcessing(_clientSetting, _parser, _logger);

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
                            }
                            else
                            {
                                _logger.Trace(exchange.Id, () => $"Complete on success, closing connection ...");
                                // should close connection 
                            }
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
                _lastActivity = ITimingProvider.Default.Instant();
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