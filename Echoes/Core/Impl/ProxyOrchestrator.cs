using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Echoes.H2;
using Echoes.IO;

namespace Echoes.Core
{
    internal class ProxyOrchestrator : IDisposable
    {
        private readonly Func<Exchange, Task> _exchangeListener;
        private readonly Func<string, Stream> _throttlePolicy;
        private readonly ProxyStartupSetting _startupSetting;
        private readonly ClientSetting _clientSetting;
        private readonly ExchangeBuilder _exchangeBuilder;
        private readonly PoolBuilder _poolBuilder;

        public ProxyOrchestrator(
            Func<Exchange, Task> exchangeListener,
            Func<string, Stream> throttlePolicy,
            ProxyStartupSetting startupSetting,
            ClientSetting clientSetting,
            ExchangeBuilder exchangeBuilder,
            PoolBuilder poolBuilder)
        {
            _exchangeListener = exchangeListener;
            _throttlePolicy = throttlePolicy;
            _startupSetting = startupSetting;
            _clientSetting = clientSetting;
            _exchangeBuilder = exchangeBuilder;
            _poolBuilder = poolBuilder;
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
                        localConnection = await _exchangeBuilder.InitClientConnection(client.GetStream(), buffer, token);
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


                    var shouldClose = false;


                    do
                    {
                        if (exchange != null &&
                            !exchange.Request.Header.Method.Span.Equals("connect", StringComparison.OrdinalIgnoreCase))
                        {
                            // Check whether the local browser ask for a connection close 

                            shouldClose = exchange.Request
                                .Header["Connection".AsMemory()].Any(c =>
                                    c.Value.Span.Equals("close", StringComparison.OrdinalIgnoreCase));

                            IHttpConnectionPool connectionPool = null;

                            try
                            {
                                while (true)
                                {
                                    // get a connection pool for the current exchange 
                                    // the connection pool may 

                                    connectionPool = await _poolBuilder.GetPool(exchange, _clientSetting, token);

                                    try
                                    {
                                        await connectionPool.Send(exchange, localConnection, token);
                                    }
                                    catch (ConnectionCloseException)
                                    {
                                        // This connection was "goawayed" while current exchange 
                                        // tries to use it. 
                                        
                                        // let a chance for the _poolbuilder to release it
                                        
                                        await Task.Yield(); 

                                        continue;
                                    }
                                    // Actual request send 
                                    

                                    break; 
                                }

                            }
                            catch (Exception exception)
                            {
                                // The caller cancelled the task 

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

                            if (!exchange.Request.Header.IsWebSocketRequest)
                            {
                                // Request processed by IHttpConnectionPool returns before complete response body
                                
                                if (exchange.Response.Header.ContentLength == -1 &&
                                    exchange.Response.Body != null &&
                                    exchange.HttpVersion == "HTTP/2")
                                {
                                    // In HTTP2, server is allowed to send a response body
                                    // without specifying a content-length or transfer-encoding chunked.
                                    // In case content-length is not present, we force transfer-encoding chunked 
                                    // to allowed HTTP/1.1 client to know the end of the content body

                                    exchange.Response.Header.ForceTransferChunked();
                                }

                                // Writing the received header to downstream

                                if (DebugContext.InsertEchoesMetricsOnResponseHeader)
                                {
                                    exchange.Response.Header.AddExtraHeaderFieldToLocalConnection(
                                        exchange.GetMetricsSummaryAsHeader());
                                }

                                var intHeaderCount = exchange.Response.Header.WriteHttp11(buffer, true, true);

                                // headerContent = Encoding.ASCII.GetString(buffer, 0, intHeaderCount);
                                
                                if (_exchangeListener != null)
                                {
                                    await _exchangeListener(exchange);
                                }

                                try
                                {
                                    // Start sending response to browser

                                    await localConnection.WriteStream.WriteAsync(
                                        new ReadOnlyMemory<byte>(buffer, 0, intHeaderCount),
                                        token);
                                }
                                catch (TaskCanceledException)
                                {
                                    break;
                                }
                                catch (IOException)
                                {
                                    // local connection interrupt

                                    break;
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
                                            // Local connection may close the underlying connection before 
                                            // receiving the entire message. In that case, we just leave
                                            // without any error

                                            if (ex is IOException && ex.InnerException is SocketException sex)
                                            {
                                                if (sex.SocketErrorCode == SocketError.ConnectionAborted)
                                                    callerTokenSource.Cancel();
                                            }

                                            break;
                                        }

                                        throw;
                                    }
                                }

                                // In case the down stream connection is persisted, 
                                // we wait for the current exchange to complete before reading further request

                                try
                                {

                                    shouldClose = shouldClose || await exchange.Complete;
                                }
                                catch (ExchangeException ex)
                                {
                                    // Enhance your calm
                                }
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
                                buffer, token
                            );
                        }
                        catch (IOException ioEx)
                        {
                            // Downstream close the underlying connection
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

        public void Dispose()
        {
        }
    }
}