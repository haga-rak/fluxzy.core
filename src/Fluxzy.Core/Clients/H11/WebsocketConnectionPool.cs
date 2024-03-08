// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Misc.ResizableBuffers;
using Fluxzy.Misc.Streams;
using Fluxzy.Writers;

namespace Fluxzy.Clients.H11
{
    internal class WebsocketConnectionPool : IHttpConnectionPool
    {
        private readonly RemoteConnectionBuilder _connectionBuilder;
        private readonly DnsResolutionResult _dnsResolutionResult;
        private readonly ProxyRuntimeSetting _proxyRuntimeSetting;
        private readonly SemaphoreSlim _semaphoreSlim;
        private readonly ITimingProvider _timingProvider;

        public WebsocketConnectionPool(
            Authority authority,
            ITimingProvider timingProvider,
            RemoteConnectionBuilder connectionBuilder,
            ProxyRuntimeSetting proxyRuntimeSetting, DnsResolutionResult dnsResolutionResult)
        {
            _timingProvider = timingProvider;
            _connectionBuilder = connectionBuilder;
            _proxyRuntimeSetting = proxyRuntimeSetting;
            _dnsResolutionResult = dnsResolutionResult;
            Authority = authority;
            _semaphoreSlim = new SemaphoreSlim(proxyRuntimeSetting.ConcurrentConnection + 100);
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
            Exchange exchange, ILocalLink localLink, RsBuffer buffer,
            CancellationToken cancellationToken = default)
        {
            try {
                await _semaphoreSlim.WaitAsync(cancellationToken).ConfigureAwait(false);

                await using var ex = new WebSocketProcessing(
                    Authority, _timingProvider,
                    _connectionBuilder,
                    _proxyRuntimeSetting, _proxyRuntimeSetting.ArchiveWriter, _dnsResolutionResult);

                await ex.Process(exchange, localLink, buffer.Buffer, cancellationToken).ConfigureAwait(false);
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

    internal class WebSocketProcessing : IAsyncDisposable
    {
        private readonly RealtimeArchiveWriter? _archiveWriter;
        private readonly Authority _authority;
        private readonly ProxyRuntimeSetting _creationSetting;
        private readonly DnsResolutionResult _dnsResolutionResult;
        private readonly RemoteConnectionBuilder _remoteConnectionBuilder;
        private readonly ITimingProvider _timingProvider;

        public WebSocketProcessing(
            Authority authority,
            ITimingProvider timingProvider,
            RemoteConnectionBuilder remoteConnectionBuilder,
            ProxyRuntimeSetting creationSetting,
            RealtimeArchiveWriter? archiveWriter, DnsResolutionResult dnsResolutionResult)
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

        public async Task Process(
            Exchange exchange, ILocalLink localLink, byte[] buffer,
            CancellationToken cancellationToken)
        {
            if (localLink == null)
                throw new ArgumentNullException(nameof(localLink));

            var openingResult = await _remoteConnectionBuilder.OpenConnectionToRemote(
                exchange, _dnsResolutionResult,
                new List<SslApplicationProtocol> { SslApplicationProtocol.Http11 },
                _creationSetting,
                cancellationToken).ConfigureAwait(false);

            exchange.Connection = openingResult.Connection;

            _archiveWriter?.Update(exchange.Connection, cancellationToken);

            exchange.Metrics.RequestHeaderSending = ITimingProvider.Default.Instant();

            // Writing 
            var headerLength = exchange.Request.Header.WriteHttp11(buffer, false);
            await exchange.Connection.WriteStream!.WriteAsync(buffer, 0, headerLength, cancellationToken).ConfigureAwait(false);

            exchange.Metrics.RequestHeaderSent = ITimingProvider.Default.Instant();

            using var rsBuffer = RsBuffer.Allocate(FluxzySharedSetting.RequestProcessingBuffer);

            var headerBlock = await
                Http11HeaderBlockReader.GetNext(exchange.Connection.ReadStream!, rsBuffer,
                    () => exchange.Metrics.ResponseHeaderStart = ITimingProvider.Default.Instant(),
                    () => exchange.Metrics.ResponseHeaderEnd = ITimingProvider.Default.Instant(),
                    throwOnError: false, cancellationToken);

            Memory<char> headerContent = new char[headerBlock.HeaderLength];

            Encoding.ASCII
                    .GetChars(rsBuffer.Memory.Slice(0, headerBlock.HeaderLength).Span, headerContent.Span);

            exchange.Response.Header = new ResponseHeader(
                headerContent, exchange.Authority.Secure, true);

            await localLink.WriteStream!.WriteAsync(rsBuffer.Buffer, 0, headerBlock.HeaderLength, cancellationToken).ConfigureAwait(false);

            var concatedReadStream = exchange.Connection.ReadStream!;

            if (headerBlock.HeaderLength < headerBlock.TotalReadLength) {
                var remainder = new byte[headerBlock.TotalReadLength -
                                         headerBlock.HeaderLength];

                Buffer.BlockCopy(rsBuffer.Buffer, headerBlock.HeaderLength,
                    remainder, 0, remainder.Length);

                // Concat the extra body bytes read while retrieving header
                concatedReadStream = new CombinedReadonlyStream(
                    true,
                    new MemoryStream(remainder),
                    exchange.Connection.ReadStream!
                );
            }

            _archiveWriter?.Update(exchange, ArchiveUpdateType.AfterResponseHeader, cancellationToken);

            try {
                var outWriteStream = exchange.Connection.WriteStream;

                var addLock = new object();

                await using var upReadStream = new WebSocketStream(concatedReadStream, _timingProvider,
                    cancellationToken,
                    wsMessage => {
                        wsMessage.Direction = WsMessageDirection.Receive;

                        lock (addLock) {
                            exchange.WebSocketMessages ??= new List<WsMessage>();
                            exchange.WebSocketMessages.Add(wsMessage);
                        }

                        _archiveWriter!.Update(exchange, ArchiveUpdateType.WsMessageReceived, CancellationToken.None);
                    },
                    wsMessageId => _archiveWriter!.CreateWebSocketResponseContent(exchange.Id, wsMessageId));

                await using var downReaderStream = new WebSocketStream(localLink.ReadStream!, _timingProvider,
                    cancellationToken,
                    wsMessage => {
                        wsMessage.Direction = WsMessageDirection.Sent;

                        lock (addLock) {
                            exchange.WebSocketMessages ??= new List<WsMessage>();
                            exchange.WebSocketMessages.Add(wsMessage);
                        }

                        _archiveWriter!.Update(exchange, ArchiveUpdateType.WsMessageSent, CancellationToken.None);
                    },
                    wsMessageId => _archiveWriter!.CreateWebSocketRequestContent(exchange.Id, wsMessageId));

                var copyTask = Task.WhenAny(
                    downReaderStream.CopyDetailed(outWriteStream, buffer, copied =>
                            exchange.Metrics.TotalSent += copied
                        , cancellationToken).AsTask(),
                    upReadStream.CopyDetailed(localLink.WriteStream, 1024 * 16, copied =>
                            exchange.Metrics.TotalReceived += copied
                        , cancellationToken).AsTask());

                await copyTask.ConfigureAwait(false);
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
                exchange.ExchangeCompletionSource.TrySetResult(true);
            }
        }

        public void Feed(ReadOnlySpan<byte> data)
        {
        }

        public void OnWsMessageAvailable(WsMessage message)
        {
        }
    }

    public enum WsOpCode
    {
        Continuation = 0,
        Text = 1,
        Binary = 2,
        ConnectionClose = 8,
        Ping = 9,
        Pong = 0xA,
        PingBinary = Ping | Binary,
        PingText = Ping | Text,
        PongBinary = Pong | Binary,
        PongText = Pong | Text
    }

    public static class WsOpCodeUtils
    {
        public static WsOpCode RemoveControlFlags(this WsOpCode original)
        {
            original = original & ~WsOpCode.ConnectionClose;

            return original;
        }
    }
}
