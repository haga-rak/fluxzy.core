// Copyright © 2021 Haga Rakotoharivelo

using System;
using System.Buffers;
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
        private readonly RemoteConnectionBuilder _connectionBuilder;
        private readonly ClientSetting _clientSetting;
        private SemaphoreSlim _semaphoreSlim; 

        public TunnelOnlyConnectionPool(
            Authority authority, 
            ITimingProvider timingProvider,
            RemoteConnectionBuilder connectionBuilder,
            ClientSetting clientSetting)
        {
            _timingProvider = timingProvider;
            _connectionBuilder = connectionBuilder;
            _clientSetting = clientSetting;
            Authority = authority;
            _semaphoreSlim = new SemaphoreSlim(clientSetting.ConcurrentConnection); 
        }

        public Authority Authority { get; }

        public bool Complete => false;

        public Task Init()
        {
            return Task.CompletedTask; 
        }

        public async ValueTask Send(
            Exchange exchange, ILocalLink localLink, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _semaphoreSlim.WaitAsync(cancellationToken);

                using (var ex = new TunneledConnectionProcess(
                           Authority, _timingProvider,
                           _connectionBuilder, 
                           _clientSetting))
                {
                    await ex.Process(exchange, localLink, CancellationToken.None); 
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
        private readonly RemoteConnectionBuilder _remoteConnectionBuilder;
        private readonly ClientSetting _creationSetting;
        private readonly int _bufferSize;

        public TunneledConnectionProcess(Authority authority,
            ITimingProvider timingProvider,
            RemoteConnectionBuilder remoteConnectionBuilder,
            ClientSetting creationSetting,
            int bufferSize = 1024 * 16 )
        {
            _authority = authority;
            _timingProvider = timingProvider;
            _remoteConnectionBuilder = remoteConnectionBuilder;
            _creationSetting = creationSetting;
            _bufferSize = bufferSize;
        }

        public async Task Process(Exchange exchange, ILocalLink localLink, CancellationToken cancellationToken)
        {
            if (localLink == null)
                throw new ArgumentNullException(nameof(localLink));

            var openingResult = await _remoteConnectionBuilder.OpenConnectionToRemote(exchange.Authority, true,
                new List<SslApplicationProtocol> { SslApplicationProtocol.Http11 },
                _creationSetting,
                cancellationToken).ConfigureAwait(false);
            
            exchange.Connection = openingResult.Connection;

            if (exchange.Request.Header.IsWebSocketRequest)
            {
                var buffer = ArrayPool<byte>.Shared.Rent(exchange.Request.Header.HeaderLength);

                try
                {
                    var headerLength = exchange.Request.Header.WriteHttp11(buffer, false);

                    await exchange.Connection.WriteStream.WriteAsync(buffer, 0, headerLength, cancellationToken);

                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }

            try
            {
                await using var remoteStream = exchange.Connection.WriteStream;

                var copyTask = Task.WhenAll(
                    localLink.ReadStream.CopyDetailed(remoteStream, _bufferSize, (copied) =>
                            exchange.Metrics.TotalSent += copied
                        , cancellationToken).AsTask(),
                    remoteStream.CopyDetailed(localLink.WriteStream, _bufferSize, (copied) =>
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