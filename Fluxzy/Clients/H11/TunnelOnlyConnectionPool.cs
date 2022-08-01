// Copyright © 2021 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Misc;

namespace Fluxzy.Clients.H11
{
    internal class TunnelOnlyConnectionPool : IHttpConnectionPool
    {
        private readonly ITimingProvider _timingProvider;
        private readonly RemoteConnectionBuilder _connectionBuilder;
        private readonly ProxyRuntimeSetting _proxyRuntimeSetting;
        private SemaphoreSlim _semaphoreSlim;
        private bool _complete;

        public TunnelOnlyConnectionPool(
            Authority authority, 
            ITimingProvider timingProvider,
            RemoteConnectionBuilder connectionBuilder,
            ProxyRuntimeSetting proxyRuntimeSetting)
        {
            _timingProvider = timingProvider;
            _connectionBuilder = connectionBuilder;
            _proxyRuntimeSetting = proxyRuntimeSetting;
            Authority = authority;
            _semaphoreSlim = new SemaphoreSlim(proxyRuntimeSetting.ConcurrentConnection); 
        }

        public Authority Authority { get; }

        public bool Complete => _complete;

        public Task Init()
        {
            return Task.CompletedTask; 
        }

        public Task<bool> CheckAlive()
        {
            return Task.FromResult(!Complete); 
        }

        public async ValueTask Send(
            Exchange exchange, ILocalLink localLink, byte [] buffer,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _semaphoreSlim.WaitAsync(cancellationToken);

                await using var ex = new TunneledConnectionProcess(
                    Authority, _timingProvider,
                    _connectionBuilder, 
                    _proxyRuntimeSetting, null);

                await ex.Process(exchange, localLink, buffer, CancellationToken.None);
            }
            finally
            {
                _semaphoreSlim.Release();
                _complete = true; 
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

    internal class TunneledConnectionProcess : IDisposable, IAsyncDisposable
    {
        private readonly Authority _authority;
        private readonly ITimingProvider _timingProvider;
        private readonly RemoteConnectionBuilder _remoteConnectionBuilder;
        private readonly ProxyRuntimeSetting _creationSetting;
        private readonly RealtimeArchiveWriter _archiveWriter;

        public TunneledConnectionProcess(Authority authority,
            ITimingProvider timingProvider,
            RemoteConnectionBuilder remoteConnectionBuilder,
            ProxyRuntimeSetting creationSetting,
            RealtimeArchiveWriter archiveWriter)
        {
            _authority = authority;
            _timingProvider = timingProvider;
            _remoteConnectionBuilder = remoteConnectionBuilder;
            _creationSetting = creationSetting;
            _archiveWriter = archiveWriter;
        }

        public async Task Process(Exchange exchange, ILocalLink localLink, byte[] buffer, CancellationToken cancellationToken)
        {
            if (localLink == null)
                throw new ArgumentNullException(nameof(localLink));

            var openingResult = await _remoteConnectionBuilder.OpenConnectionToRemote(
                exchange.Authority, 
                exchange.TunneledOnly,
                new List<SslApplicationProtocol> { SslApplicationProtocol.Http11 },
                _creationSetting,
                cancellationToken).ConfigureAwait(false);
            
            exchange.Connection = openingResult.Connection;

            if (_archiveWriter != null)
                await _archiveWriter.Update(exchange.Connection, cancellationToken);

            if (exchange.Request.Header.IsWebSocketRequest)
            {
                var headerLength = exchange.Request.Header.WriteHttp11(buffer, false);
                await exchange.Connection.WriteStream.WriteAsync(buffer, 0, headerLength, cancellationToken);
            }

            try
            {
                await using var remoteStream = exchange.Connection.WriteStream;

                var copyTask = Task.WhenAll(
                    localLink.ReadStream.CopyDetailed(remoteStream, buffer, (copied) =>
                            exchange.Metrics.TotalSent += copied
                        , cancellationToken).AsTask(),
                    remoteStream.CopyDetailed(localLink.WriteStream, 1024*16, (copied) =>
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