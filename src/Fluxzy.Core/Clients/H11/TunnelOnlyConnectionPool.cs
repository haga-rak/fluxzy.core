// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Misc.ResizableBuffers;
using Fluxzy.Misc.Streams;
using Fluxzy.Writers;

namespace Fluxzy.Clients.H11
{
    internal class TunnelOnlyConnectionPool : IHttpConnectionPool
    {
        private readonly RemoteConnectionBuilder _connectionBuilder;
        private readonly ProxyRuntimeSetting _proxyRuntimeSetting;
        private readonly DnsResolutionResult _resolutionResult;
        private readonly SemaphoreSlim _semaphoreSlim;
        private readonly ITimingProvider _timingProvider;

        public TunnelOnlyConnectionPool(
            Authority authority,
            ITimingProvider timingProvider,
            RemoteConnectionBuilder connectionBuilder,
            ProxyRuntimeSetting proxyRuntimeSetting, DnsResolutionResult resolutionResult)
        {
            _timingProvider = timingProvider;
            _connectionBuilder = connectionBuilder;
            _proxyRuntimeSetting = proxyRuntimeSetting;
            _resolutionResult = resolutionResult;
            Authority = authority;
            _semaphoreSlim = new SemaphoreSlim(proxyRuntimeSetting.ConcurrentConnection);
        }
        

        public Authority Authority { get; }

        public bool Complete { get; private set; }

        public void Init()
        {
        }

        public ValueTask<bool> CheckAlive()
        {
            return new ValueTask<bool>(!Complete);
        }

        public async ValueTask Send(
            Exchange exchange, IDownStreamPipe downStreamPipe, RsBuffer buffer, ExchangeScope __,
            CancellationToken cancellationToken = default)
        {
            try {
                await _semaphoreSlim.WaitAsync(cancellationToken).ConfigureAwait(false);

                await using var ex = new TunneledConnectionProcess(
                    Authority, _timingProvider,
                    _connectionBuilder,
                    _proxyRuntimeSetting, null, _resolutionResult);

                await ex.Process(exchange, downStreamPipe, buffer.Buffer, CancellationToken.None).ConfigureAwait(false);
            }
            finally {
                _semaphoreSlim.Release();
                Complete = true;
            }
        }

        public ValueTask DisposeAsync()
        {
            _semaphoreSlim.Dispose();

            return new ValueTask(Task.CompletedTask);
        }
    }

    internal class TunneledConnectionProcess : IDisposable, IAsyncDisposable
    {
        private readonly RealtimeArchiveWriter? _archiveWriter;
        private readonly DnsResolutionResult _dnsResolutionResult;
        private readonly Authority _authority;
        private readonly ProxyRuntimeSetting _creationSetting;
        private readonly RemoteConnectionBuilder _remoteConnectionBuilder;
        private readonly ITimingProvider _timingProvider;

        public TunneledConnectionProcess(
            Authority authority,
            ITimingProvider timingProvider,
            RemoteConnectionBuilder remoteConnectionBuilder,
            ProxyRuntimeSetting creationSetting,
            RealtimeArchiveWriter? archiveWriter, 
            DnsResolutionResult dnsResolutionResult)
        {
            _authority = authority;
            _timingProvider = timingProvider;
            _remoteConnectionBuilder = remoteConnectionBuilder;
            _creationSetting = creationSetting;
            _archiveWriter = archiveWriter;
            _dnsResolutionResult = dnsResolutionResult;
        }

        public ValueTask DisposeAsync()
        {
            return default;
        }

        public void Dispose()
        {
        }

        public async Task Process(
            Exchange exchange, IDownStreamPipe downStreamPipe, byte[] buffer,
            CancellationToken cancellationToken)
        {
            var openingResult = await _remoteConnectionBuilder.OpenConnectionToRemote(
                exchange, _dnsResolutionResult,
                new List<SslApplicationProtocol> { SslApplicationProtocol.Http11 },
                _creationSetting, exchange.Context.ProxyConfiguration,
                cancellationToken).ConfigureAwait(false);

            exchange.Connection = openingResult.Connection;

            _archiveWriter?.Update(exchange.Connection, cancellationToken);

            if (exchange.Request.Header.IsWebSocketRequest) {
                var headerLength = exchange.Request.Header.WriteHttp11(false, buffer, false);
                await exchange.Connection.WriteStream!.WriteAsync(buffer, 0, headerLength, cancellationToken).ConfigureAwait(false);
            }

            try {
                await using var remoteStream = exchange.Connection.WriteStream;
                using var haltTokenSource = new CancellationTokenSource();
                var copyTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                    haltTokenSource.Token,
                    cancellationToken);

                var (readStream, writeStream) = downStreamPipe.AbandonPipe();

                await using var _ = readStream;
                await using var __ = writeStream;

                var tasks = new Task[] {
                    readStream.CopyDetailed(remoteStream!, buffer, copied =>
                            exchange.Metrics.TotalSent += copied
                        , copyTokenSource.Token).AsTask(),
                    remoteStream!.CopyDetailed(writeStream, 1024 * 16, copied =>
                            exchange.Metrics.TotalReceived += copied
                        , copyTokenSource.Token).AsTask()
                };

                await Task.WhenAny(tasks).ConfigureAwait(false);

                haltTokenSource.Cancel();

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch (Exception ex) {
                if (ex is IOException || ex is SocketException) {
                    exchange.Errors.Add(new Error("", ex));

                    return;
                }

                throw;
            }
            finally {
                exchange.Metrics.RemoteClosed = _timingProvider.Instant();
            }
        }
    }
}
