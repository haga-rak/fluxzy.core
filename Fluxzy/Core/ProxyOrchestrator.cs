using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Clients.H2;
using Fluxzy.Misc;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Core
{
    internal class ProxyOrchestrator : IDisposable
    {
        private readonly ProxyRuntimeSetting _proxyRuntimeSetting;
        private readonly ExchangeBuilder _exchangeBuilder;
        private readonly PoolBuilder _poolBuilder;
        private readonly RealtimeArchiveWriter _archiveWriter;
        private readonly IExchangeEventSource _eventSource;
        private readonly ProxyExecutionContext _executionContext;

        public ProxyOrchestrator(
            ProxyRuntimeSetting proxyRuntimeSetting,
            ExchangeBuilder exchangeBuilder,
            PoolBuilder poolBuilder,
            RealtimeArchiveWriter archiveWriter,
            IExchangeEventSource eventSource)
        {
            _proxyRuntimeSetting = proxyRuntimeSetting;
            _exchangeBuilder = exchangeBuilder;
            _poolBuilder = poolBuilder;
            _archiveWriter = archiveWriter;
            _executionContext = proxyRuntimeSetting.ExecutionContext;
            _eventSource = eventSource;
        }

        public async Task Operate(TcpClient client, byte [] buffer, CancellationToken token)
        {
            try
            {
                using var callerTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);

                token = callerTokenSource.Token;

                if (!token.IsCancellationRequested)
                {
                    // READ initial state of connection, 
                    ExchangeBuildingResult localConnection = null;

                    try
                    {
                        localConnection = await _exchangeBuilder.InitClientConnection(
                            client.GetStream(), buffer, _proxyRuntimeSetting, token);
                    }
                    catch (Exception ex)
                    {
                        // Failure from the local connection

                        if (ex is SocketException || ex is IOException)
                            return; 
                    }

                    if (localConnection == null)
                        return;

                    Exchange exchange =
                        localConnection.ProvisionalExchange;

                    var endPoint = (IPEndPoint) client.Client.RemoteEndPoint;

                    exchange.Metrics.LocalPort = endPoint.Port; 
                    exchange.Metrics.LocalAddress = endPoint.Address.ToString(); 

                    var shouldClose = false;


                    do
                    {
                        if (exchange != null &&
                            (!exchange.Request.Header.Method.Span.Equals("connect", StringComparison.OrdinalIgnoreCase)
                             || localConnection.TunnelOnly)
                           )
                        {
                            // Check whether the local browser ask for a connection close 

                            shouldClose = exchange.Request
                                .Header["Connection".AsMemory()].Any(c =>
                                    c.Value.Span.Equals("close", StringComparison.OrdinalIgnoreCase));

                            _eventSource.OnBeforeRequest(new BeforeRequestEventArgs(_executionContext, exchange));

                            IHttpConnectionPool connectionPool;

                            try
                            {
                                while (true)
                                {
                                    await _proxyRuntimeSetting.EnforceRules(exchange.Context, 
                                        FilterScope.RequestHeaderReceivedFromClient,
                                        exchange.Connection, exchange);

                                    if (_archiveWriter != null)
                                    {
                                        await _archiveWriter.Update(
                                            exchange,
                                            CancellationToken.None
                                        );

                                        if (exchange.Request.Body != null &&
                                            (!exchange.Request.Body.CanSeek || exchange.Request.Body.Length > 0))
                                        {
                                            exchange.Request.Body = new DispatchStream(exchange.Request.Body,
                                                true,
                                                _archiveWriter.CreateRequestBodyStream(exchange.Id));
                                        }
                                    }

                                    // get a connection pool for the current exchange 
                                    // the connection pool may 

                                    connectionPool = await _poolBuilder.GetPool(exchange, _proxyRuntimeSetting, token);

                                    // Actual request send 

                                    try
                                    {

                                        await connectionPool.Send(exchange, localConnection, buffer, token);
                                    }
                                    catch (ConnectionCloseException)
                                    {
                                        // This connection was "goawayed" while current exchange 
                                        // tries to use it. 

                                        // let a chance for the _poolbuilder to release it

                                        await Task.Yield();

                                        continue;
                                    }
                                    finally
                                    {
                                        // We close the request body dispatchstream
                                        await SafeCloseRequestBody(exchange);
                                    }

                                    

                                    break; 
                                }

                            }
                            catch (Exception exception)
                            {
                                // The caller cancelled the task 

                                await SafeCloseRequestBody(exchange);
                                await SafeCloseResponseBody(exchange);

                                if (exception is OperationCanceledException)
                                    break;

                                if (!ConnectionErrorHandler
                                        .RequalifyOnResponseSendError(exception, exchange))
                                {
                                    throw;
                                }

                                shouldClose = true;
                            }

                            // We do not need to read websocket response

                            if (!exchange.Request.Header.IsWebSocketRequest && !exchange.Context.BlindMode
                                && exchange.Response.Header != null)
                            {
                                // Request processed by IHttpConnectionPool returns before complete response body

                                await _proxyRuntimeSetting.EnforceRules(exchange.Context,
                                    FilterScope.ResponseHeaderReceivedFromRemote,
                                    exchange.Connection, exchange);

                                if (exchange.Response.Header.ContentLength == -1 &&
                                    exchange.Response.Body != null &&
                                    exchange.HttpVersion == "HTTP/2")
                                {
                                    // HTTP2 server is allowed to send a response body
                                    // without specifying a content-length or transfer-encoding chunked.
                                    // In case content-length is not present, we force transfer-encoding chunked 
                                    // in order to inform HTTP/1.1 receiver of the content body end

                                    exchange.Response.Header.ForceTransferChunked();
                                }

                                // Writing the received header to downstream

                                if (DebugContext.InsertFluxzyMetricsOnResponseHeader)
                                {
                                    exchange.Response.Header.AddExtraHeaderFieldToLocalConnection(
                                        exchange.GetMetricsSummaryAsHeader());
                                }

                                var responseHeaderLength = exchange.Response.Header.WriteHttp11(buffer, true, true);

                                _eventSource.OnBeforeResponse(new BeforeResponseEventArgs(_executionContext, exchange));

                                if (_archiveWriter != null)
                                {
                                    // Update the state of the exchange 
                                    await _archiveWriter.Update(exchange,
                                        CancellationToken.None
                                    );

                                    if (exchange.Response.Body != null &&
                                        (!exchange.Response.Body.CanSeek || exchange.Response.Body.Length > 0))
                                    {
                                        exchange.Response.Body = new DispatchStream(exchange.Response.Body,
                                            true,
                                            _archiveWriter.CreateResponseBodyStream(exchange.Id));
                                    }
                                }

                                try
                                {
                                    // Start sending response to browser

                                    await localConnection.WriteStream.WriteAsync(
                                        new ReadOnlyMemory<byte>(buffer, 0, responseHeaderLength),
                                        token);
                                }
                                catch (Exception ex)
                                {
                                    await SafeCloseRequestBody(exchange);
                                    await SafeCloseResponseBody(exchange);

                                    if (ex is OperationCanceledException || ex is IOException)
                                    {
                                        // local browser interrupt connection 

                                        break; 
                                    }

                                    throw;
                                }

                                if (exchange.Response.Header.ContentLength != 0 &&
                                    exchange.Response.Body != null)
                                {
                                    var localConnectionWriteStream = localConnection.WriteStream;

                                    if (exchange.Response.Header.ChunkedBody)
                                    {
                                        localConnectionWriteStream =
                                            new ChunkedTransferWriteStream(localConnectionWriteStream);
                                    }

                                    try
                                    {
                                        await exchange.Response.Body.CopyDetailed(
                                            localConnectionWriteStream, buffer, _ => { }, token);

                                        (localConnectionWriteStream as ChunkedTransferWriteStream)?.WriteEof();

                                        await localConnection.WriteStream.FlushAsync(CancellationToken.None);
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
                                                    callerTokenSource.Cancel();
                                                }
                                            }

                                            break;
                                        }

                                        throw;
                                    }
                                    finally
                                    {
                                        await SafeCloseRequestBody(exchange);
                                        await SafeCloseResponseBody(exchange);

                                        _eventSource.OnExchangeComplete(new ExchangeCompleteEventArgs(
                                            _executionContext, exchange));
                                    }
                                }
                                else
                                {
                                    if (exchange.Response.Body != null)
                                    {
                                        await SafeCloseRequestBody(exchange);
                                        await SafeCloseResponseBody(exchange);
                                    }
                                }

                                // In case the down stream connection is persisted, 
                                // we wait for the current exchange to complete before reading further request

                                try
                                {

                                    shouldClose = shouldClose || await exchange.Complete;
                                }
                                catch (ExchangeException)
                                {
                                    // Enhance your calm
                                }
                            }
                            else
                            {
                                shouldClose = true; 
                            }

                            // Handle websocket request here and produce result 
                        }

                        if (shouldClose)
                        {
                            break;
                        }

                        try
                        {
                            // Read the nex HTTP message 
                            exchange = await _exchangeBuilder.ReadExchange(
                                localConnection.ReadStream,
                                localConnection.Authority,
                                buffer, _proxyRuntimeSetting, token
                            );

                            if (exchange != null && exchange.Metrics != null)
                            {
                                var ep2 = (IPEndPoint)client.Client.RemoteEndPoint;

                                exchange.Metrics.LocalPort = ep2.Port;
                                exchange.Metrics.LocalAddress = ep2.Address.ToString();
                            }
                        }
                        catch (IOException)
                        {
                            // Downstream close the underlying connection
                            break; 
                        }

                    } while (exchange != null);

                }
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                {
                    return;
                }

                // FATAL exception only happens here 
                throw;
            }
            finally
            {

            }
        }

        private async Task SafeCloseRequestBody(Exchange exchange)
        {
            if (exchange.Request.Body != null)
            {
                try
                {
                    // Clean the pipe 

                    await exchange.Request.Body.DisposeAsync();
                    exchange.Request.Body = null;
                }
                catch
                {
                    // ignore errors when closing pipe 
                }
            }
        }

        private async Task SafeCloseResponseBody(Exchange exchange)
        {
            if (exchange.Response.Body != null)
            {
                try
                {
                    // Clean the pipe 

                    await exchange.Response.Body.DisposeAsync();
                    exchange.Response.Body = null;
                }
                catch
                {
                    // ignore errors when closing pipe 
                }
            }
        }

        public void Dispose()
        {
        }
    }
}