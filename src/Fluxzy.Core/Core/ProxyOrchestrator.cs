// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Clients.H2;
using Fluxzy.Extensions;
using Fluxzy.Misc.ResizableBuffers;
using Fluxzy.Misc.Streams;
using Fluxzy.Misc.Traces;
using Fluxzy.Rules;
using Fluxzy.Writers;
using Org.BouncyCastle.Tls;

namespace Fluxzy.Core
{
    internal class ProxyOrchestrator : IDisposable
    {
        private readonly RealtimeArchiveWriter _archiveWriter;
        private readonly FromProxyConnectSourceProvider _fromProxyConnectSourceProvider;
        private readonly PoolBuilder _poolBuilder;
        private readonly ProxyRuntimeSetting _proxyRuntimeSetting;
        private readonly ExchangeContextBuilder _exchangeContextBuilder;

        public ProxyOrchestrator(
            ProxyRuntimeSetting proxyRuntimeSetting,
            FromProxyConnectSourceProvider fromProxyConnectSourceProvider,
            PoolBuilder poolBuilder)
        {
            _proxyRuntimeSetting = proxyRuntimeSetting;
            _fromProxyConnectSourceProvider = fromProxyConnectSourceProvider;
            _poolBuilder = poolBuilder;
            _archiveWriter = proxyRuntimeSetting.ArchiveWriter;
            _exchangeContextBuilder = new ExchangeContextBuilder(proxyRuntimeSetting);
        }

        public void Dispose()
        {
        }

        public async ValueTask Operate(TcpClient client, RsBuffer buffer, bool closeImmediately, CancellationToken token)
        {
            try {

                if (D.EnableTracing)
                {
                    var message = $"Receive from {client.Client.RemoteEndPoint}";
                    D.TraceInfo(message);
                }

                using var callerTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);

                token = callerTokenSource.Token;

                if (!token.IsCancellationRequested) {
                    // READ initial state of connection, 
                    ExchangeSourceInitResult? localConnection = null;

                    try {
                        localConnection = await _fromProxyConnectSourceProvider.InitClientConnection(client.GetStream(), buffer,
                            _exchangeContextBuilder, token);
                    }
                    catch (Exception ex) {
                        // Failure from the local connection

                        if (D.EnableTracing)
                        {
                            var message = $"Client connection failure {client.Client.RemoteEndPoint}";
                            D.TraceException(ex, message);
                        }

                        var errorInfo = DownstreamErrorInfo.CreateFrom(client, ex);

                        _archiveWriter.Update(errorInfo, token);

                        // Logs into the secure logger

                        if (ex is SocketException || ex is IOException)
                            return;
                    }

                    if (localConnection == null)
                        return;

                    var exchange =
                        localConnection.ProvisionalExchange;

                    var endPoint = (IPEndPoint) client.Client.RemoteEndPoint!;
                    var localEndPoint = (IPEndPoint) client.Client.LocalEndPoint!;

                    exchange.Metrics.DownStreamClientPort = endPoint.Port;
                    exchange.Metrics.DownStreamClientAddress = endPoint.Address.ToString();
                    exchange.Metrics.DownStreamLocalPort = localEndPoint.Port;
                    exchange.Metrics.DownStreamLocalAddress = localEndPoint.Address.ToString();
                    exchange.Context.DownStreamLocalAddressStruct = localEndPoint.Address;
                    exchange.Context.ProxyListenPort = _proxyRuntimeSetting.ProxyListenPort;

                    var shouldClose = false;

                    do {
                        if (
                            !exchange.Request.Header.Method.Span.Equals("connect", StringComparison.OrdinalIgnoreCase)
                            || localConnection.TunnelOnly
                        ) {
                            // Check whether the local browser ask for a connection close 

                            if (D.EnableTracing) {
                                var message = $"[#{exchange.Id}] Processing {exchange.Request.Header.Authority}";
                                D.TraceInfo(message);
                            }

                            shouldClose = exchange.ShouldClose() || closeImmediately;

                            if (_proxyRuntimeSetting.UserAgentProvider != null) {
                                var userAgentValue = exchange.GetRequestHeaderValue("User-Agent");

                                // Solve user agent 

                                exchange.Agent = Agent.Create(userAgentValue ?? string.Empty,
                                    ((IPEndPoint) client.Client.LocalEndPoint!).Address,
                                    _proxyRuntimeSetting.UserAgentProvider);
                            }

                            exchange.Step = ExchangeStep.Request;

                            await _proxyRuntimeSetting.EnforceRules(exchange.Context,
                                FilterScope.RequestHeaderReceivedFromClient,
                                exchange.Connection, exchange);

                            if (exchange.Context.BreakPointContext != null) {
                                await exchange.Context.BreakPointContext.ConnectionSetupCompletion
                                              .WaitForEdit();
                            }

                            // Run header alteration 

                            foreach (var requestHeaderAlteration in exchange.Context.RequestHeaderAlterations) {
                                requestHeaderAlteration.Apply(exchange.Request.Header);
                            }

                            IHttpConnectionPool connectionPool;

                            try {
                                if (exchange.Context.BreakPointContext != null) {
                                    await exchange.Context.BreakPointContext.RequestHeaderCompletion
                                                  .WaitForEdit();
                                }

                                if (_archiveWriter != null) {
                                    _archiveWriter.Update(
                                        exchange,
                                        ArchiveUpdateType.BeforeRequestHeader,
                                        CancellationToken.None
                                    );

                                    if (exchange.Request.Body != null &&
                                        (!exchange.Request.Body.CanSeek || exchange.Request.Body.Length > 0)) {
                                        exchange.Request.Body = new DispatchStream(exchange.Request.Body,
                                            true,
                                            _archiveWriter.CreateRequestBodyStream(exchange.Id));
                                    }
                                }

                                while (true) {
                                    // get a connection pool for the current exchange 

                                    connectionPool = await _poolBuilder.GetPool(exchange, _proxyRuntimeSetting, token);


                                    if (D.EnableTracing)
                                    {
                                        var message = $"[#{exchange.Id}] Pool received";
                                        D.TraceInfo(message);
                                    }


                                    // Actual request send 

                                    try
                                    {
                                        await connectionPool.Send(exchange, localConnection, buffer, token);

                                        if (D.EnableTracing)
                                        {
                                            var message = $"[#{exchange.Id}] Response received";
                                            D.TraceInfo(message);
                                        }
                                    }
                                    catch (Exception ex) {

                                        if (ex is ConnectionCloseException || ex is TlsFatalAlert) {
                                            // This connection was "goawayed" while current exchange 
                                            // tries to use it. 

                                            // let a chance for the _poolbuilder to release it

                                            await Task.Yield();

                                            continue;
                                        }

                                        throw;
                                    }
                                    finally {
                                        // We close the request body dispatchstream
                                        await SafeCloseRequestBody(exchange);
                                    }

                                    break;
                                }
                            }
                            catch (Exception exception) {
                                // The caller cancelled the task 

                                await SafeCloseRequestBody(exchange);
                                await SafeCloseResponseBody(exchange);

                                if (exception is OperationCanceledException)
                                    break;

                                if (!ConnectionErrorHandler.RequalifyOnResponseSendError(exception, exchange,
                                        ITimingProvider.Default))
                                    throw;

                                shouldClose = true;
                            }

                            // We do not need to read websocket response

                            if (!exchange.Request.Header.IsWebSocketRequest && !exchange.Context.BlindMode
                                                                            && exchange.Response.Header != null) {
                                // Request processed by IHttpConnectionPool returns before complete response body
                                // Apply response alteration 

                                await _proxyRuntimeSetting.EnforceRules(exchange.Context,
                                    FilterScope.ResponseHeaderReceivedFromRemote,
                                    exchange.Connection, exchange);

                                // Setup break point for response 

                                if (exchange.Context.BreakPointContext != null) {
                                    await exchange.Context.BreakPointContext.ResponseHeaderCompletion
                                                  .WaitForEdit();
                                }
                                if (exchange.Response.Header.ContentLength == -1 &&
                                    exchange.Response.Body != null &&
                                    exchange.HttpVersion == "HTTP/2")

                                    // HTTP2 servers are allowed to send response body
                                    // without specifying a content-length or transfer-encoding chunked.
                                    // In case content-length is not present, we force transfer-encoding chunked 
                                    // in order to inform HTTP/1.1 receiver of the content body end

                                    exchange.Response.Header.ForceTransferChunked();

                                foreach (var responseHeaderAlteration in exchange.Context.ResponseHeaderAlterations) {
                                    responseHeaderAlteration.Apply(exchange.Response.Header);
                                }

                                // Writing the received header to downstream

                                if (DebugContext.InsertFluxzyMetricsOnResponseHeader) {
                                    exchange.Response.Header?.AddExtraHeaderFieldToLocalConnection(
                                        exchange.GetMetricsSummaryAsHeader());
                                }
                                
                                var responseHeaderLength = exchange.Response.Header!.WriteHttp11(buffer, true, true, shouldClose);

                                if (_archiveWriter != null) {
                                    // Update the state of the exchange
                                    // 
                                    _archiveWriter.Update(exchange, ArchiveUpdateType.AfterResponseHeader,
                                        CancellationToken.None
                                    );

                                    if (exchange.Response.Body != null &&
                                        (!exchange.Response.Body.CanSeek || exchange.Response.Body.Length > 0)) {
                                        var dispatchStream = new DispatchStream(exchange.Response.Body,
                                            true,
                                            _archiveWriter.CreateResponseBodyStream(exchange.Id));

                                        var ext = exchange;

                                        dispatchStream.OnDisposeDoneTask = () => {
                                            _archiveWriter.Update(ext,
                                                ArchiveUpdateType.AfterResponse,
                                                CancellationToken.None
                                            );

                                            if (D.EnableTracing)
                                            {
                                                var message = $"[#{ext.Id}] Response body done";
                                                D.TraceInfo(message);
                                            }

                                            return default;
                                        };

                                        exchange.Response.Body = dispatchStream;
                                    }
                                    else {
                                        // No response body, we ensure the stream is done

                                        _archiveWriter.Update(exchange,
                                            ArchiveUpdateType.AfterResponse,
                                            CancellationToken.None
                                        );

                                    }
                                }

                                try {
                                    // Start sending response to browser
                                    await localConnection.WriteStream.WriteAsync(
                                        new ReadOnlyMemory<byte>(buffer.Buffer, 0, responseHeaderLength),
                                        token);
                                }
                                catch (Exception ex) {
                                    await SafeCloseRequestBody(exchange);
                                    await SafeCloseResponseBody(exchange);

                                    if (ex is OperationCanceledException || ex is IOException)

                                        // local browser interrupt connection 
                                        break;

                                    throw;
                                }

                                if (exchange.Response.Header.ContentLength != 0 &&
                                    exchange.Response.Body != null) {
                                    var localConnectionWriteStream = localConnection.WriteStream;

                                    if (exchange.Response.Header.ChunkedBody) {
                                        localConnectionWriteStream =
                                            new ChunkedTransferWriteStream(localConnectionWriteStream);
                                    }

                                    try {
                                        await exchange.Response.Body.CopyDetailed(
                                            localConnectionWriteStream, buffer.Buffer, _ => { }, token);

                                        (localConnectionWriteStream as ChunkedTransferWriteStream)?.WriteEof();

                                        await localConnection.WriteStream.FlushAsync(CancellationToken.None);
                                    }
                                    catch (Exception ex) {
                                        if (ex is IOException || ex is OperationCanceledException) {
                                            // Local connection may close the underlying stream before 
                                            // receiving the entire message. Particulary when waiting for the last 0\r\n\r\n on chunked stream.
                                            // In that case, we just leave
                                            // without any error

                                            if (ex is IOException && ex.InnerException is SocketException sex) {
                                                if (sex.SocketErrorCode == SocketError.ConnectionAborted)
                                                    callerTokenSource.Cancel();
                                            }

                                            break;
                                        }

                                        throw;
                                    }
                                    finally {
                                        await SafeCloseRequestBody(exchange);
                                        await SafeCloseResponseBody(exchange);
                                    }
                                }
                                else {
                                    if (exchange.Response.Body != null) {
                                        await SafeCloseRequestBody(exchange);
                                        await SafeCloseResponseBody(exchange);
                                    }
                                }

                                // In case the down stream connection is persisted, 
                                // we wait for the current exchange to complete before reading further request

                                try {
                                    shouldClose = shouldClose || await exchange.Complete;
                                }
                                catch (ExchangeException) {
                                    // Enhance your calm
                                }
                            }
                            else
                                shouldClose = true;

                            // Handle websocket request here and produce result 
                        }

                        if (shouldClose)
                            break;

                        try {
                            // Read the next HTTP message 
                            exchange = await _fromProxyConnectSourceProvider.ReadNextExchange(
                                localConnection.ReadStream,
                                localConnection.Authority,
                                buffer, _exchangeContextBuilder, token
                            );

                            if (exchange != null) {
                                var ep2 = (IPEndPoint) client.Client.RemoteEndPoint!;

                                exchange.Metrics.DownStreamClientPort = ep2.Port;
                                exchange.Metrics.DownStreamClientAddress = ep2.Address.ToString();
                            }
                        }
                        catch (IOException ex) {
                            // Downstream close the underlying connection

                            if (D.EnableTracing)
                            {
                                var message = $"Error from {client.Client.RemoteEndPoint}";
                                D.TraceException(ex, message);
                            }

                            break;
                        }
                    }
                    while (exchange != null);
                }
            }
            catch (Exception ex) {

                if (ex is OperationCanceledException) {

                    if (D.EnableTracing)
                    {
                        var message = $"Error from {client.Client.RemoteEndPoint}";
                        D.TraceException(ex, message);
                    }

                    return;
                }


                // FATAL exception only happens here 
                throw;
            }
        }

        private async ValueTask SafeCloseRequestBody(Exchange exchange)
        {
            if (exchange.Request.Body != null) {
                try {
                    // Clean the pipe 
                    await exchange.Request.Body.DisposeAsync();
                    exchange.Request.Body = null;
                }
                catch {
                    // ignore errors when closing pipe 
                }
            }
        }

        private async ValueTask SafeCloseResponseBody(Exchange exchange)
        {
            if (exchange.Response.Body != null) {
                try {
                    // Clean the pipe 
                    await exchange.Response.Body.DisposeAsync();
                    exchange.Response.Body = null;
                }
                catch {
                    // ignore errors when closing pipe 
                }
            }
        }
    }
}
