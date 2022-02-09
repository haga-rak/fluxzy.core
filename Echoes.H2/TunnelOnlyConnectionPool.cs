// Copyright © 2021 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Echoes.H2.IO;

namespace Echoes.H2
{
    public class TunnelOnlyConnectionPool : IHttpConnectionPool
    {
        private readonly ITimingProvider _timingProvider;
        private readonly int _maxConcurrentConnection;
        private SemaphoreSlim _semaphoreSlim; 

        public TunnelOnlyConnectionPool(
            Authority authority, 
            ITimingProvider timingProvider,
            int maxConcurrentConnection)
        {
            _timingProvider = timingProvider;
            _maxConcurrentConnection = maxConcurrentConnection;
            Authority = authority;
            _semaphoreSlim = new SemaphoreSlim(maxConcurrentConnection); 
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

                using (var ex = new TunneledConnectionProcess(Authority, _timingProvider))
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
        private readonly TunnelSetting _creationSetting;
        private readonly int _bufferSize;

        public TunneledConnectionProcess(Authority authority,
            ITimingProvider timingProvider,
            IRemoteConnectionBuilder remoteConnectionBuilder,
            TunnelSetting creationSetting,
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
                    exchange.BaseStream.CopyAndReturnCopied(remoteStream, _bufferSize, (copied) =>
                            exchange.Metrics.TotalSent += copied
                        , cancellationToken),
                    remoteStream.CopyAndReturnCopied(exchange.BaseStream, _bufferSize, (copied) =>
                            exchange.Metrics.TotalReceived += copied
                        , cancellationToken));

                await copyTask.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (ex is IOException || ex is SocketException)
                {
                    exchange.Errors.Add(new Error(ex));
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

    public interface ITimingProvider
    {
        DateTime Instant(); 
    }
}