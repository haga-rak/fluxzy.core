// Copyright © 2021 Haga Rakotoharivelo

using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Echoes.H2
{
    public class TunnelOnlyConnectionPool : IHttpConnectionPool
    {
        private readonly int _maxConcurrentConnection;
        private SemaphoreSlim _semaphoreSlim; 

        public TunnelOnlyConnectionPool(Authority authority, int maxConcurrentConnection)
        {
            _maxConcurrentConnection = maxConcurrentConnection;
            Authority = authority;
            _semaphoreSlim = new SemaphoreSlim(maxConcurrentConnection); 
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
                await _semaphoreSlim.WaitAsync(cancellationToken);

                using (var ex = new TunneledConnectionProcess(Authority))
                {
                    await ex.Processs(exchange, CancellationToken.None); 
                }
            }
            finally
            {
                _semaphoreSlim.Release(); 
            }
        }


        public ValueTask DisposeAsync()
        {
        }

        public void Dispose()
        {
        }

    }

    public class TunneledConnectionProcess : IDisposable, IAsyncDisposable
    {
        private readonly Authority _authority;
        private readonly int _bufferSize;
        private TcpClient _client; 

        public TunneledConnectionProcess(Authority authority, int bufferSize = 1024 * 16 )
        {
            _authority = authority;
            _bufferSize = bufferSize;
        }

        private static async long CopyAndReturnCopied(Stream source , Stream destination)
        {
            long totalCopied = 0; 


        }


        public async Task Processs(Exchange exchange, CancellationToken cancellationToken)
        {
            if (exchange.BaseStream == null)
                throw new ArgumentNullException(nameof(exchange.BaseStream)); 

            if (_client == null)
            {
                _client = new TcpClient();
                await _client.ConnectAsync(_authority.HostName, _authority.Port); 
            }

            var remoteStream = _client.GetStream();

            var copyTask = Task.WhenAll(exchange.BaseStream.CopyToAsync(remoteStream, cancellationToken),
                remoteStream.CopyToAsync(exchange.BaseStream, cancellationToken));

            await copyTask.ConfigureAwait(false); 
        }

        public void Dispose()
        {
            _client?.Dispose(); 
        }

        public ValueTask DisposeAsync()
        {
        }
    }
}