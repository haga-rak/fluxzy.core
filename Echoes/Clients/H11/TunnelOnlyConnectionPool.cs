// Copyright © 2021 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Echoes.IO;

namespace Echoes.H11
{
    public class TunnelOnlyConnectionPool : IHttpConnectionPool
    {
        private readonly ITimingProvider _timingProvider;
        private readonly IRemoteConnectionBuilder _connectionBuilder;
        private readonly ClientSetting _clientSetting;
        private SemaphoreSlim _semaphoreSlim; 

        public TunnelOnlyConnectionPool(
            Authority authority, 
            ITimingProvider timingProvider,
            IRemoteConnectionBuilder connectionBuilder,
            ClientSetting clientSetting)
        {
            _timingProvider = timingProvider;
            _connectionBuilder = connectionBuilder;
            _clientSetting = clientSetting;
            Authority = authority;
            _semaphoreSlim = new SemaphoreSlim(clientSetting.ConcurrentConnection); 
        }

        public Authority Authority { get; }

        public Task Init()
        {
            return Task.CompletedTask; 
        }

        public async ValueTask Send(Exchange exchange, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _semaphoreSlim.WaitAsync(cancellationToken);

                using (var ex = new TunneledConnectionProcess(Authority, _timingProvider, _connectionBuilder, _clientSetting))
                {
                    await ex.Process(exchange, CancellationToken.None); 
                }
            }
            finally
            {
                _semaphoreSlim.Release(); 
            }
        }

        public ValueTask DisposeAsync()
        {
            return new ValueTask(Task.CompletedTask); 
        }

        public void Dispose()
        {
            _semaphoreSlim.Dispose();
        }
    }

    public class TunneledConnectionProcess : IDisposable, IAsyncDisposable
    {
        private readonly Authority _authority;
        private readonly ITimingProvider _timingProvider;
        private readonly IRemoteConnectionBuilder _remoteConnectionBuilder;
        private readonly ClientSetting _creationSetting;
        private readonly int _bufferSize;

        public TunneledConnectionProcess(Authority authority,
            ITimingProvider timingProvider,
            IRemoteConnectionBuilder remoteConnectionBuilder,
            ClientSetting creationSetting,
            int bufferSize = 1024 * 16 )
        {
            _authority = authority;
            _timingProvider = timingProvider;
            _remoteConnectionBuilder = remoteConnectionBuilder;
            _creationSetting = creationSetting;
            _bufferSize = bufferSize;
        }


        public async Task Process(Exchange exchange, CancellationToken cancellationToken)
        {
            if (exchange.BaseStream == null)
                throw new ArgumentNullException(nameof(exchange.BaseStream));

            await _remoteConnectionBuilder.OpenConnectionToRemote(exchange, true,
                new List<SslApplicationProtocol> { SslApplicationProtocol.Http11 },
                _creationSetting,
                cancellationToken
            ).ConfigureAwait(false);

            try
            {
                await using var remoteStream = exchange.UpStream;

                var copyTask = Task.WhenAll(
                    exchange.BaseStream.CopyDetailed(remoteStream, _bufferSize, (copied) =>
                            exchange.Metrics.TotalSent += copied
                        , cancellationToken).AsTask(),
                    remoteStream.CopyDetailed(exchange.BaseStream, _bufferSize, (copied) =>
                            exchange.Metrics.TotalReceived += copied
                        , cancellationToken).AsTask());

                await copyTask.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (ex is IOException || ex is SocketException)
                {
                    exchange.Errors.Add(new Error("", ex));
                    return;
                }

                throw;
            }
            finally
            {
                exchange.Metrics.RemoteClosed = _timingProvider.Instant();
            }
        }

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync()
        {
            return new ValueTask(Task.CompletedTask); 
        }
    }
}