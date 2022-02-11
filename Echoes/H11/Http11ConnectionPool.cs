// Copyright © 2021 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Echoes.H2;
using Echoes.H2.Encoder.Utils;

namespace Echoes.H11
{
    public class Http11ConnectionPool : IHttpConnectionPool
    {
        private static readonly List<SslApplicationProtocol> Http11Protocols = new List<SslApplicationProtocol>() { SslApplicationProtocol.Http11 };
        
        private readonly IRemoteConnectionBuilder _remoteConnectionBuilder;
        private readonly ITimingProvider _timingProvider;
        private readonly GlobalSetting _globalSetting;
        private readonly Http11Parser _parser;
        private readonly SemaphoreSlim _semaphoreSlim;
        private readonly Queue<Http11ProcessingState> _processingStates = new Queue<Http11ProcessingState>(); 

        public Http11ConnectionPool(
            Authority authority, 
            Stream existingStream, 
            IRemoteConnectionBuilder remoteConnectionBuilder,
            ITimingProvider timingProvider,
            GlobalSetting globalSetting, 
            Http11Parser parser)
        {
            _remoteConnectionBuilder = remoteConnectionBuilder;
            _timingProvider = timingProvider;
            _globalSetting = globalSetting;
            _parser = parser;
            Authority = authority;
            _semaphoreSlim = new SemaphoreSlim(globalSetting.ConcurrentConnection);

            if (existingStream != null)
            {
                _processingStates.Enqueue(new Http11ProcessingState(existingStream, _timingProvider));
            }
        }

        public Authority Authority { get; }

        public Task Init()
        {
            return Task.CompletedTask; 
        }

        public async ValueTask Send(Exchange exchange, CancellationToken cancellationToken)
        {
            try
            {
                // Looks like we are already sure that this connection is gonna be HTTP/2

                await _semaphoreSlim.WaitAsync(cancellationToken).ConfigureAwait(false);

                lock (this)
                {
                    DateTime requestDate = _timingProvider.Instant(); 

                    while (_processingStates.TryDequeue(out var state))
                    {
                        if ((requestDate - state.LastUsed) > TimeSpan.FromSeconds(_globalSetting.TimeOutSecondsUnusedConnection))
                        {
                            // The connection pool exceeds timing connection 
                            continue; 
                        }

                        exchange.UpStream = state.Stream; 
                    }
                }

                if (exchange.UpStream == null)
                {
                    await _remoteConnectionBuilder.OpenConnectionToRemote(exchange, false, Http11Protocols,
                        _globalSetting, cancellationToken);
                }

                var poolProcessing = new Http11PoolProcessing(_timingProvider, _globalSetting, _parser);

                try
                {
                    await poolProcessing.Process(exchange, cancellationToken)
                        .ConfigureAwait(false);
                    
                    var res = exchange.Complete
                        .ContinueWith(completeTask =>
                        {
                            
                            if (completeTask.Exception != null && completeTask.Exception.InnerExceptions.Any())
                            {
                                foreach (var exception in completeTask.Exception.InnerExceptions)
                                {
                                    exchange.Errors.Add(new Error("Error while reading resp", exception));
                                }
                            }
                            else if (completeTask.IsCompletedSuccessfully && !completeTask.Result)
                            {
                                _processingStates.Enqueue(new Http11ProcessingState(exchange.UpStream, _timingProvider));
                            }
                        }, cancellationToken);
                }
                catch (Exception ex)
                {
                    if (ex is SocketException ||
                        ex is IOException || 
                        ex is ExchangeException)
                    {
                        exchange.Errors.Add(new Error("Error while reading resp", ex));

                        if (exchange.UpStream != null)
                        {
                            await exchange.UpStream.DisposeAsync();
                        }

                    }
                    else
                        throw;
                }
            }
            finally
            {
                _semaphoreSlim.Release(); 
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

        public Http11ProcessingState(Stream stream, ITimingProvider timingProvider)
        {
            _stream = stream;
            _timingProvider = timingProvider;
            LastUsed = _timingProvider.Instant();
            
        }

        public DateTime LastUsed { get; }

        public Stream Stream => _stream;
    }
}