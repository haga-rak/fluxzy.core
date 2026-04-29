// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Clients.Mock;
using Fluxzy.Extensions;
using Fluxzy.Logging;
using Fluxzy.Misc.ResizableBuffers;
using Fluxzy.Misc.Streams;
using Fluxzy.Rules;
using Fluxzy.Utils.ProcessTracking;
using Fluxzy.Writers;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Tls;

namespace Fluxzy.Core
{
    internal class ProxyOrchestrator : IDisposable
    {
        private readonly RealtimeArchiveWriter _archiveWriter;
        private readonly ExchangeSourceProvider _exchangeSourceProvider;
        private readonly PoolBuilder _poolBuilder;
        private readonly ProxyRuntimeSetting _proxyRuntimeSetting;
        private readonly ILogger<ProxyOrchestrator> _logger;

        public ProxyOrchestrator(
            ProxyRuntimeSetting proxyRuntimeSetting,
            ExchangeSourceProvider exchangeSourceProvider,
            PoolBuilder poolBuilder)
        {
            _proxyRuntimeSetting = proxyRuntimeSetting;
            _exchangeSourceProvider = exchangeSourceProvider;
            _poolBuilder = poolBuilder;
            _archiveWriter = proxyRuntimeSetting.ArchiveWriter;
            _logger = proxyRuntimeSetting.GetLogger<ProxyOrchestrator>();
        }

        public void Dispose()
        {
        }

        private static string SafeRemoteEndPoint(TcpClient client)
        {
            try {
                return client.Client.RemoteEndPoint?.ToString() ?? string.Empty;
            }
            catch {
                return string.Empty;
            }
        }

        public async ValueTask Operate(
            TcpClient client, RsBuffer buffer, bool closeImmediately, CancellationToken token)
        {
            var lastExchangeHolder = new ExchangeHolder();

            IDownStreamPipe? downStreamPipe = null;

            try
            {
                using var callerTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);

                token = callerTokenSource.Token;

                ExchangeSourceInitResult? exchangeSourceInitResult = null;

                try
                {
                    exchangeSourceInitResult = await _exchangeSourceProvider.InitClientConnection(
                                                                                client.GetStream(), buffer,
                                                                                (IPEndPoint)client.Client
                                                                                    .LocalEndPoint!,
                                                                                (IPEndPoint)client.Client
                                                                                    .RemoteEndPoint!, token)
                                                                            .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // Failure from the local connection

                    var remote = SafeRemoteEndPoint(client);
                    FluxzyLogEvents.ClientConnectionInitFailed(_logger, ex, remote);

                    var errorInfo = DownstreamErrorInfo.CreateFrom(client, ex);

                    _archiveWriter.Update(errorInfo, token);

                    if (ex is SocketException || ex is IOException)
                    {
                        return;
                    }
                }

                if (exchangeSourceInitResult == null)
                {
                    return;
                }

                var remoteEndPoint = (IPEndPoint)client.Client.RemoteEndPoint!;
                var localEndPoint = (IPEndPoint)client.Client.LocalEndPoint!;
                var downStreamClientAddress = remoteEndPoint.Address.ToString();
                var localEndPointsAddress = localEndPoint.Address.ToString();

                downStreamPipe = exchangeSourceInitResult.DownStreamPipe;
                var provisionalExchange = exchangeSourceInitResult.ProvisionalExchange;

                if (downStreamPipe.SupportsMultiplexing) {
                    await OperateMultiplexed(
                        buffer, closeImmediately, token,
                        provisionalExchange, downStreamPipe,
                        remoteEndPoint, downStreamClientAddress,
                        localEndPoint, localEndPointsAddress,
                        callerTokenSource);
                }
                else {
                    await OperateSequential(
                        buffer, closeImmediately, token,
                        provisionalExchange, downStreamPipe,
                        remoteEndPoint, downStreamClientAddress,
                        localEndPoint, localEndPointsAddress,
                        callerTokenSource, client, lastExchangeHolder);
                }
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                {
                    return;
                }

                var handleResult = await
                    ConnectionErrorHandler.HandleGenericException(ex, downStreamPipe,
                        lastExchangeHolder.Exchange, buffer, _archiveWriter, ITimingProvider.Default, token);

                if (!handleResult)
                {
                    throw;
                }
            }
            finally
            {
                downStreamPipe?.Dispose();
            }
        }

        private async ValueTask OperateSequential(
            RsBuffer buffer, bool closeImmediately, CancellationToken token,
            Exchange? provisionalExchange, IDownStreamPipe downStreamPipe,
            IPEndPoint remoteEndPoint, string downStreamClientAddress,
            IPEndPoint localEndPoint, string localEndPointsAddress,
            CancellationTokenSource callerTokenSource, TcpClient client,
            ExchangeHolder lastExchangeHolder)
        {
            var processedProvisional = false;

            while (true)
            {
                using var exchangeScope = new ExchangeScope();

                Exchange? exchange = null;

                try
                {
                    if (!processedProvisional)
                    {
                        exchange = provisionalExchange;
                        processedProvisional = true;
                    }
                    else
                    {
                        exchange = await downStreamPipe.ReadNextExchange(buffer, exchangeScope, token)
                                                       .ConfigureAwait(false);
                    }

                    lastExchangeHolder.Exchange = exchange;
                }
                catch (IOException)
                {
                    // Client closed mid-read — normal connection-close path.
                    return;
                }

                if (exchange == null)
                {
                    return;
                }

                // Bridge the upstream pool to the downstream pipe so it can
                // forward interim (1xx) responses — notably `100 Continue`
                // for `Expect: 100-continue` (issue #624).
                var capturedPipe = downStreamPipe;
                var capturedStreamId = exchange.StreamIdentifier;
                exchange.InterimResponseWriter = (statusCode, reason, ct) =>
                    capturedPipe.WriteInterimResponse(statusCode, reason, capturedStreamId, ct);

                var shouldCloseDownStreamConnection = await EnterProcessExchange(
                    buffer, closeImmediately, token, exchange,
                    remoteEndPoint, downStreamClientAddress,
                    localEndPoint, localEndPointsAddress,
                    downStreamPipe, callerTokenSource);

                if (shouldCloseDownStreamConnection)
                {
                    return;
                }
            }
        }

        private async ValueTask OperateMultiplexed(
            RsBuffer buffer, bool closeImmediately, CancellationToken token,
            Exchange? provisionalExchange, IDownStreamPipe downStreamPipe,
            IPEndPoint remoteEndPoint, string downStreamClientAddress,
            IPEndPoint localEndPoint, string localEndPointsAddress,
            CancellationTokenSource callerTokenSource)
        {
            var tracker = new ActiveExchangeTracker();
            var processedProvisional = false;

            while (true)
            {
                Exchange? exchange = null;

                try
                {
                    if (!processedProvisional)
                    {
                        exchange = provisionalExchange;
                        processedProvisional = true;
                    }
                    else
                    {
                        using var exchangeScope = new ExchangeScope();
                        exchange = await downStreamPipe.ReadNextExchange(buffer, exchangeScope, token)
                                                       .ConfigureAwait(false);
                    }
                }
                catch (IOException)
                {
                    break;
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                if (exchange == null)
                {
                    break;
                }

                // Skip the provisional CONNECT/SOCKS5 exchange (Unprocessed = true)
                if (exchange.Unprocessed)
                {
                    continue;
                }

                // Same interim-response bridge as the sequential path. The H2
                // downstream implementation is a no-op, so this is defensive
                // coverage for mixed-version scenarios (H2 client, H1 upstream).
                var capturedPipe = downStreamPipe;
                var capturedStreamId = exchange.StreamIdentifier;
                exchange.InterimResponseWriter = (statusCode, reason, ct) =>
                    capturedPipe.WriteInterimResponse(statusCode, reason, capturedStreamId, ct);

                tracker.Increment();

                ProcessExchangeMultiplexed(
                    exchange, closeImmediately, token,
                    remoteEndPoint, downStreamClientAddress,
                    localEndPoint, localEndPointsAddress,
                    downStreamPipe, callerTokenSource,
                    tracker);
            }

            // Wait for all in-flight exchanges to complete
            // (the caller will dispose the pipe after this)
            await tracker.WaitForAll().ConfigureAwait(false);
        }

        private async void ProcessExchangeMultiplexed(
            Exchange exchange, bool closeImmediately, CancellationToken token,
            IPEndPoint remoteEndPoint, string downStreamClientAddress,
            IPEndPoint localEndPoint, string localEndPointsAddress,
            IDownStreamPipe downStreamPipe, CancellationTokenSource callerTokenSource,
            ActiveExchangeTracker tracker)
        {
            using var exchangeBuffer = RsBuffer.Allocate(FluxzySharedSetting.RequestProcessingBuffer);

            try
            {
                await EnterProcessExchange(
                    exchangeBuffer, closeImmediately, token, exchange,
                    remoteEndPoint, downStreamClientAddress,
                    localEndPoint, localEndPointsAddress,
                    downStreamPipe, callerTokenSource);
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                {
                    return;
                }

                // Per-exchange error: try to send an error response for this stream only
                try
                {
                    await ConnectionErrorHandler.HandleGenericException(
                        ex, downStreamPipe, exchange, exchangeBuffer,
                        _archiveWriter, ITimingProvider.Default, token);
                }
                catch
                {
                    // Swallow — one stream's error must not kill the connection
                }
            }
            finally
            {
                tracker.Decrement();
            }
        }

        private async ValueTask<bool> EnterProcessExchange(
            RsBuffer buffer, bool closeImmediately, CancellationToken token, Exchange exchange, IPEndPoint remoteEndPoint,
            string downStreamClientAddress,
            IPEndPoint localEndPoint, string localEndPointsAddress, IDownStreamPipe downStreamPipe,
            CancellationTokenSource callerTokenSource)
        {
            UpdateExchangeMetrics(exchange, remoteEndPoint, downStreamClientAddress, localEndPoint,
                localEndPointsAddress);

            using var exchangeLogScope = FluxzyLoggerScopes.BeginExchangeScope(_logger, exchange);

            if (!exchange.Unprocessed && _proxyRuntimeSetting.UserAgentProvider != null)
            {
                var userAgentValue = exchange.GetRequestHeaderValue("User-Agent");

                // Solve user agent
                exchange.Agent = Agent.Create(userAgentValue ?? string.Empty, localEndPoint.Address,
                    _proxyRuntimeSetting.UserAgentProvider);
            }

            // Collect process info if enabled and connection is from localhost
            if (_proxyRuntimeSetting.StartupSetting.EnableProcessTracking
                && IPAddress.IsLoopback(remoteEndPoint.Address))
            {
                exchange.ProcessInfo = ProcessTracker.Instance.GetProcessInfo(remoteEndPoint.Port);
            }

            if (!exchange.Unprocessed)
            {
                exchange.LogActivity = FluxzyActivitySource.StartExchangeActivity(exchange);
                FluxzyLogEvents.LogRequestResolutionStarted(_logger, exchange);
            }

            var shouldCloseDownStreamConnection = await InternalProcessExchange(exchange,
                downStreamPipe, buffer, closeImmediately, callerTokenSource, token);

            return shouldCloseDownStreamConnection;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateExchangeMetrics(
            Exchange exchange, IPEndPoint remoteEndPoint, string downStreamClientAddress, IPEndPoint localEndPoint,
            string localEndPointsAddress)
        {
            exchange.Metrics.DownStreamClientPort = remoteEndPoint.Port;
            exchange.Metrics.DownStreamClientAddress = downStreamClientAddress;
            exchange.Metrics.DownStreamLocalPort = localEndPoint.Port;
            exchange.Metrics.DownStreamLocalAddress = localEndPointsAddress;
            exchange.Context.DownStreamLocalAddressStruct = localEndPoint.Address;
            exchange.Context.ProxyListenPort = _proxyRuntimeSetting.ProxyListenPort;
            exchange.Context.DownStreamLocalAddressStruct = localEndPoint.Address;
            exchange.Context.ProxyListenPort = _proxyRuntimeSetting.ProxyListenPort;
        }

        private async Task<bool> InternalProcessExchange(
            Exchange exchange,
            IDownStreamPipe downStreamPipe,
            RsBuffer buffer,
            bool closeImmediately,
            CancellationTokenSource callerTokenSource, CancellationToken token)
        {
            var shouldCloseConnectionToDownStream = false;
            using var exchangeScope = new ExchangeScope();

            var processMessage = !exchange.Unprocessed;

            if (processMessage)
            {
                // Check whether the local browser ask for a connection close

                shouldCloseConnectionToDownStream = closeImmediately || exchange.ShouldClose();

                exchange.Step = ExchangeStep.Request;

                await _proxyRuntimeSetting.EnforceRules(exchange.Context,
                    FilterScope.RequestHeaderReceivedFromClient,
                    exchange.Connection, exchange).ConfigureAwait(false);

                if (exchange.Context.Abort)
                {
                    return true;
                }

                if (exchange.Context.BreakPointContext != null)
                {
                    await exchange.Context.BreakPointContext.ConnectionSetupCompletion
                                  .WaitForEdit().ConfigureAwait(false);
                }

                // Run header alteration 

                foreach (var requestHeaderAlteration in exchange.Context.RequestHeaderAlterations)
                {
                    requestHeaderAlteration.Apply(exchange.Request.Header);
                }

                Stream? originalRequestBodyStream = null;
                Stream? originalResponseBodyStream = null;

                try
                {
                    if (exchange.Context.BreakPointContext != null)
                    {
                        await exchange.Context.BreakPointContext.RequestHeaderCompletion
                                      .WaitForEdit().ConfigureAwait(false);
                    }

                    var hasRequestBody = exchange.Request.Body != null &&
                                         (!exchange.Request.Body.CanSeek ||
                                          exchange.Request.Body.Length > 0);

                    exchange.Context.HasRequestBody = hasRequestBody;

                    if (_archiveWriter != null!)
                    {
                        _archiveWriter.Update(
                            exchange,
                            ArchiveUpdateType.BeforeRequestHeader,
                            CancellationToken.None
                        );

                        if (exchange.Context.HasRequestBodySubstitution)
                        {
                            originalRequestBodyStream =
                                hasRequestBody ? exchange.Request.Body : Stream.Null;

                            exchange.Request.Body = await
                                exchange.Context.GetSubstitutedRequestBody(exchange.Request.Body!,
                                    exchange).ConfigureAwait(false);

                            exchange.Request.Header.ForceTransferChunked();
                        }

                        if (exchange.Request.Body != null &&
                            (!exchange.Request.Body.CanSeek ||
                             exchange.Request.Body.Length > 0))
                        {
                            exchange.Request.Body = new DispatchStream(exchange.Request.Body!,
                                true,
                                _archiveWriter.CreateRequestBodyStream(exchange.Id));
                        }
                    }

                    while (true)
                    {
                        IHttpConnectionPool connectionPool;

                        if (exchange.Context.PreMadeResponse != null)
                        {
                            connectionPool = new MockedConnectionPool(exchange.Authority,
                                exchange.Context.PreMadeResponse);

                            connectionPool.Init();
                        }
                        else
                        {
                            // get a connection pool for the current exchange
                            connectionPool = await _poolBuilder
                                                   .GetPool(exchange, _proxyRuntimeSetting, token)
                                                   .ConfigureAwait(false);
                        }

                        // Actual request send

                        try
                        {
                            await connectionPool.Send(exchange,
                                downStreamPipe,
                                buffer, exchangeScope, token).ConfigureAwait(false);

                            if (exchange.ReadUntilClose)
                            {
                                shouldCloseConnectionToDownStream = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            if (ex is ConnectionCloseException || ex is TlsFatalAlert)
                            {
                                // This connection was "goawayed" while current exchange 
                                // tries to use it. 

                                // let a chance for the _poolbuilder to release it

                                await Task.Yield();

                                continue;
                            }

                            throw;
                        }
                        finally
                        {
                            // We close the request body dispatchstream
                            await SafeCloseRequestBody(exchange, originalRequestBodyStream)
                                .ConfigureAwait(false);
                        }

                        break;
                    }
                }
                catch (Exception exception)
                {
                    // The caller cancelled the task 

                    await SafeCloseRequestBody(exchange, originalRequestBodyStream).ConfigureAwait(false);
                    await SafeCloseResponseBody(exchange, originalResponseBodyStream).ConfigureAwait(false);

                    if (exception is OperationCanceledException)
                    {
                        return shouldCloseConnectionToDownStream;
                    }

                    if (!ConnectionErrorHandler.RequalifyOnResponseSendError(exception, exchange,
                            ITimingProvider.Default))
                    {
                        throw;
                    }

                    shouldCloseConnectionToDownStream = true;
                }

                // We do not need to read websocket response

                if (!exchange.Request.Header.IsWebSocketRequest
                    && (!exchange.Context.Authority.Secure || !exchange.Context.BlindMode)
                    && exchange.Response.Header != null)
                {
                    // Request processed by IHttpConnectionPool returns before complete response body
                    // Apply response alteration 

                    await _proxyRuntimeSetting.EnforceRules(exchange.Context,
                        FilterScope.ResponseHeaderReceivedFromRemote,
                        exchange.Connection, exchange).ConfigureAwait(false);

                    // Setup break point for response 

                    if (exchange.Context.BreakPointContext != null)
                    {
                        await exchange.Context.BreakPointContext.ResponseHeaderCompletion
                                      .WaitForEdit().ConfigureAwait(false);
                    }

                    var responseBodyStream = exchange.Response.Body;

                    var responseBodyChunked = false;
                    var compressionType = CompressionType.None;

                    if (exchange.Context.HasResponseBodySubstitution)
                    {
                        responseBodyChunked = exchange.IsResponseChunkedTransferEncoded();
                        compressionType = exchange.GetResponseCompressionType();

                        if (compressionType != CompressionType.None)
                        {
                            exchange.Response.Header.RemoveHeader("content-encoding");
                        }

                        // Body substitution changes content length, so we must remove
                        // Content-Length and use chunked transfer encoding instead
                        exchange.Response.Header.RemoveHeader("content-length");
                        exchange.Response.Header.ContentLength = -1;
                    }

                    if (exchange.Response.Header.ContentLength == -1 &&
                        responseBodyStream != null &&
                        !downStreamPipe.SupportsMultiplexing)
                    // When content-length is not present (either from HTTP/2 server or due to body substitution),
                    // we force transfer-encoding chunked to inform the HTTP/1.1 downstream receiver of the content body end.
                    {
                        exchange.Response.Header.ForceTransferChunked();
                    }

                    if (downStreamPipe.SupportsMultiplexing)
                    // HTTP/2 forbids transfer-encoding (RFC 9113 §8.2.2) — strip it for H2 downstream.
                    {
                        exchange.Response.Header.RemoveHeader("transfer-encoding");
                    }

                    foreach (var responseHeaderAlteration in exchange.Context.ResponseHeaderAlterations)
                    {
                        responseHeaderAlteration.Apply(exchange.Response.Header);
                    }

                    // Writing the received header to downstream

                    if (DebugContext.InsertFluxzyMetricsOnResponseHeader)
                    {
                        exchange.Response.Header?.AddExtraHeaderFieldToLocalConnection(
                            exchange.GetMetricsSummaryAsHeader());
                    }

                    if (_archiveWriter != null)
                    {
                        // Update the state of the exchange
                        // 
                        _archiveWriter.Update(exchange, ArchiveUpdateType.AfterResponseHeader,
                            CancellationToken.None
                        );

                        if (responseBodyStream != null && (!responseBodyStream.CanSeek || responseBodyStream.Length > 0))
                        {
                            if (exchange.Context.HasResponseBodySubstitution)
                            {
                                originalResponseBodyStream = responseBodyStream;

                                responseBodyStream = await
                                    exchange.Context.GetSubstitutedResponseBody(
                                                responseBodyStream, responseBodyChunked, compressionType)
                                            .ConfigureAwait(false);
                            }

                            var dispatchStream = new DispatchStream(responseBodyStream,
                                true,
                                _archiveWriter.CreateResponseBodyStream(exchange.Id));

                            var ext = exchange;

                            dispatchStream.OnDisposeDoneTask = () => {
                                _archiveWriter.Update(ext,
                                    ArchiveUpdateType.AfterResponse,
                                    CancellationToken.None
                                );

                                return default;
                            };

                            exchange.Response.Body = dispatchStream;
                            responseBodyStream = dispatchStream;
                        }
                        else
                        {
                            // No response body, we ensure the stream is done

                            _archiveWriter.Update(exchange,
                                ArchiveUpdateType.AfterResponse,
                                CancellationToken.None
                            );
                        }
                    }

                    try
                    {
                        await downStreamPipe.WriteResponseHeader(exchange.Response.Header!, buffer, shouldCloseConnectionToDownStream, exchange.StreamIdentifier, exchange.Request.Header.Method, token)
                                            .ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        await SafeCloseRequestBody(exchange, originalRequestBodyStream)
                            .ConfigureAwait(false);

                        await SafeCloseResponseBody(exchange, originalResponseBodyStream)
                            .ConfigureAwait(false);

                        if (ex is OperationCanceledException || ex is IOException)

                        // local browser interrupt connection 
                        {
                            return shouldCloseConnectionToDownStream;
                        }

                        throw;
                    }

                    if (exchange.Response.Header!.ContentLength != 0 &&
                        responseBodyStream != null)
                    {
                        try
                        {
                            var chunked = exchange.Response.Header.ChunkedBody &&
                                          exchange.Response.Header.HasResponseBody(
                                              exchange.Request.Header.Method.Span, out _);

                            await downStreamPipe.WriteResponseBody(
                                responseBodyStream,
                                buffer, chunked, exchange.StreamIdentifier,
                                exchange.Response, token).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            if (ex is IOException || ex is OperationCanceledException)
                            {
                                // Local connection may close the underlying stream before 
                                // receiving the entire message. Particulary when waiting for the last 0\r\n\r\n on chunked stream.
                                // In that case, we just leave
                                // without any error

                                if (ex is IOException && ex.InnerException is SocketException sex)
                                {
                                    if (sex.SocketErrorCode == SocketError.ConnectionAborted)
                                    {
#if NET8_0_OR_GREATER
                                        await callerTokenSource.CancelAsync();
#else
                                                    callerTokenSource.Cancel();
#endif
                                    }
                                }

                                return false;
                            }

                            throw;
                        }
                        finally
                        {
                            await SafeCloseRequestBody(exchange, originalRequestBodyStream)
                                .ConfigureAwait(false);

                            await SafeCloseResponseBody(exchange, originalResponseBodyStream)
                                .ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        if (responseBodyStream != null)
                        {
                            await SafeCloseRequestBody(exchange, originalRequestBodyStream)
                                .ConfigureAwait(false);

                            await SafeCloseResponseBody(exchange, originalResponseBodyStream)
                                .ConfigureAwait(false);
                        }
                    }

                    // In case the down stream connection is persisted, 
                    // we wait for the current exchange to complete before reading further request

                    try
                    {
                        await exchange.Complete.ConfigureAwait(false);
                    }
                    catch (ExchangeException)
                    {
                        // Enhance your calm
                    }

                    FluxzyLogEvents.LogExchangeCompleted(_logger, exchange, _proxyRuntimeSetting.StartupSetting);
                }
                else
                {
                    shouldCloseConnectionToDownStream = true;
                }
            }

            return shouldCloseConnectionToDownStream;
        }

        private static ValueTask SafeCloseRequestBody(Exchange exchange, Stream? substitutionStream)
        {
            if (exchange.Request.Body != null)
            {
                try
                {
                    // Clean the pipe 
                    var body = exchange.Request.Body;
                    exchange.Request.Body = null;

                    return body.DisposeAsync();
                }
                catch
                {
                    // ignore errors when closing pipe 
                }
            }

            return SafeCloseExtraStream(substitutionStream);
        }

        private static ValueTask SafeCloseResponseBody(Exchange exchange, Stream? substitutionStream)
        {
            if (exchange.Response.Body != null)
            {
                try
                {
                    // Clean the pipe 
                    var body = exchange.Response.Body;
                    exchange.Response.Body = null;

                    return body.DisposeAsync();
                }
                catch
                {
                    // ignore errors when closing pipe 
                }
            }

            return SafeCloseExtraStream(substitutionStream);
        }

        private static async ValueTask SafeCloseExtraStream(params Stream?[] streams)
        {
            foreach (var stream in streams)
            {
                if (stream == null)
                {
                    continue;
                }

                try
                {
                    await stream.DisposeAsync();
                }
                catch
                {
                    // ignore errors when closing pipe 
                }
            }
        }
    }

    internal class ExchangeHolder
    {
        public Exchange? Exchange { get; set; }
    }

    internal class ActiveExchangeTracker
    {
        private int _count = 1; // sentinel prevents premature completion
        private readonly TaskCompletionSource _allDone = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Increment() => Interlocked.Increment(ref _count);

        public void Decrement()
        {
            if (Interlocked.Decrement(ref _count) == 0)
            {
                _allDone.TrySetResult();
            }
        }

        public Task WaitForAll()
        {
            // Release the sentinel
            Decrement();
            return _allDone.Task;
        }
    }
}
