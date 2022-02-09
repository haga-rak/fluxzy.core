// Copyright © 2021 Haga Rakotoharivelo

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Echoes.Encoding.Utils;

namespace Echoes.H2
{
    public class Http11ConnectionPool : IHttpConnectionPool
    {
        private readonly int _maxConcurrentConnection;
        private readonly IRemoteConnectionBuilder _remoteConnectionBuilder;
        private readonly ITimingProvider _timingProvider;
        private readonly TunnelSetting _creationSetting;
        private readonly Http11Parser _parser;
        private readonly SemaphoreSlim _semaphoreSlim;
        private readonly Queue<Http11ProcessingState> _processingStates = new Queue<Http11ProcessingState>(); 

        public Http11ConnectionPool(
            Authority authority, 
            Stream existingStream, 
            int maxConcurrentConnection,
            IRemoteConnectionBuilder remoteConnectionBuilder,
            ITimingProvider timingProvider,
            TunnelSetting creationSetting, 
            Http11Parser parser)
        {
            _maxConcurrentConnection = maxConcurrentConnection;
            _remoteConnectionBuilder = remoteConnectionBuilder;
            _timingProvider = timingProvider;
            _creationSetting = creationSetting;
            _parser = parser;
            Authority = authority;
            _semaphoreSlim = new SemaphoreSlim(_maxConcurrentConnection);

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

        public async ValueTask Send(Exchange exchange, CancellationToken cancellationToken = default)
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
                        if ((requestDate - state.LastUsed) > TimeSpan.FromSeconds(_creationSetting.TimeOutSecondsUnusedConnection))
                        {
                            // The connection pool exceeds timing connection 
                            continue; 
                        }

                        exchange.UpStream = state.Stream; 
                    }
                }

                if (exchange.UpStream == null)
                {
                    await _remoteConnectionBuilder.OpenConnectionToRemote(exchange, false, SslApplicationProtocol.Http11,
                        _creationSetting, cancellationToken); 
                }

                var poolProcessing = new Http11PoolProcessing(_timingProvider, _creationSetting, _parser);

                if (await poolProcessing.Process(exchange).ConfigureAwait(false))
                {
                    _processingStates.Enqueue(new Http11ProcessingState(exchange.UpStream, _timingProvider));
                }

            }
            finally
            {
                _semaphoreSlim.Release(); 
            }


        }

        public ValueTask DisposeAsync()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
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