﻿// Copyright © 2021 Haga Rakotoharivelo

using System;
using System.Collections.Concurrent;
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
        
        private readonly RemoteConnectionBuilder _remoteConnectionBuilder;
        private readonly ITimingProvider _timingProvider;
        private readonly ClientSetting _clientSetting;
        private readonly Http11Parser _parser;
        private readonly SemaphoreSlim _semaphoreSlim;
        private readonly Queue<Http11ProcessingState> _processingStates = new Queue<Http11ProcessingState>(); 

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

            if (existingConnection != null)
            {
                _processingStates.Enqueue(new Http11ProcessingState(existingConnection, _timingProvider));
            }
        }

        public Authority Authority { get; }

        public Task Init()
        {
            return Task.CompletedTask; 
        }

        private ConcurrentDictionary<int, int> _exchangePassed = new();

        public async ValueTask Send(Exchange exchange, ILocalLink _, CancellationToken cancellationToken)
        {
            exchange.HttpVersion = "HTTP/1.1";

            try
            {
                await _semaphoreSlim.WaitAsync(cancellationToken).ConfigureAwait(false);
                
                lock (_processingStates)
                {
                    DateTime requestDate = _timingProvider.Instant(); 

                    while (_processingStates.TryDequeue(out var state))
                    {
                        if ((requestDate - state.LastUsed) > TimeSpan.FromSeconds(_clientSetting.TimeOutSecondsUnusedConnection))
                        {
                            // The connection pool exceeds timing connection 
                            continue; 
                        }

                        exchange.Connection = state.Connection; 
                    }
                }

                lock (_exchangePassed)
                {
                    _exchangePassed.GetOrAdd(exchange.Id, _ => 0);
                    var res = _exchangePassed[exchange.Id]++;
                }

                if (exchange.Connection == null)
                {
                    var openingResult = 
                        await _remoteConnectionBuilder.OpenConnectionToRemote(exchange.Authority, false, Http11Protocols,
                        _clientSetting, cancellationToken);
                    
                    exchange.Connection = openingResult.Connection; 
                }

                var poolProcessing = new Http11PoolProcessing(_timingProvider, _clientSetting, _parser);

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
                                lock (_processingStates)
                                    _processingStates.Enqueue(new Http11ProcessingState(exchange.Connection, _timingProvider));
                            }
                        }, cancellationToken);
                }
                catch (Exception ex)
                {
                    //if (ex is SocketException ||
                    //    ex is IOException || 
                    //    ex is ExchangeException)
                    //{
                    //    exchange.Errors.Add(new Error("Error while reading response from server", ex));

                    //    if (exchange.Connection != null)
                    //    {
                    //        await exchange.Connection.ReadStream.DisposeAsync();

                    //        if (exchange.Connection.ReadStream != exchange.Connection.WriteStream)
                    //            await exchange.Connection.WriteStream.DisposeAsync();
                    //    }
                    //}
                    //else
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