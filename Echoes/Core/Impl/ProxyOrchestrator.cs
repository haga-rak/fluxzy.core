using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
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
        private readonly ProxyMessageDispatcher _dispatcher;

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
            _dispatcher = new ProxyMessageDispatcher(exchangeListener);
        }

        public async Task Operate(TcpClient client, CancellationToken token)
        {
            try
            {
                if (!token.IsCancellationRequested)
                {
                    // READ initial state of connection, 
                    ExchangeBuildingResult localConnection = null;

                    try
                    {
                        localConnection = await _exchangeBuilder.InitClientConnection(client.GetStream(), _startupSetting, token);
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

                    byte[] buffer = new byte[1024 * 32];

                    var shouldClose = false;


                    do
                    {
                        string headerContent = null;

                        if (exchange != null &&
                            !exchange.Request.Header.Method.Span.Equals("connect", StringComparison.OrdinalIgnoreCase))
                        {
                            IHttpConnectionPool connectionPool = null;
                            try
                            {
                                // opening the connection to server 
                                connectionPool = await _poolBuilder.GetPool(exchange, _clientSetting, token);

                                // Actual request send 
                                await connectionPool.Send(exchange, localConnection, token);
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
                                    // We force transfer-encoding chunked to allowed HTTP/1.1 client to know
                                    // the end of the content body

                                    exchange.Response.Header.ForceTransferChunked();
                                }

                                // Writing the received header to downstream
                                var intHeaderCount = exchange.Response.Header.WriteHttp11(buffer, true);
                                
                                // headerContent = Encoding.ASCII.GetString(buffer, 0, intHeaderCount);

                                shouldClose = exchange.Request
                                    .Header["Connection".AsMemory()].Any(c =>
                                        c.Value.Span.Equals("close", StringComparison.OrdinalIgnoreCase));

                                if (shouldClose)
                                {

                                }

                                if (_exchangeListener != null)
                                {
                                    await _exchangeListener(exchange);
                                }

                                try
                                {
                                    // Sending header response to local browser

                                    await localConnection.WriteStream.WriteAsync(
                                        new ReadOnlyMemory<byte>(buffer, 0, intHeaderCount),
                                        token);



                                    if (exchange.Authority.HostName == "docs.microsoft.com")
                                    {

                                    }
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
                            else
                            {

                            }
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
            _dispatcher.Dispose();
        }
    }


    public class ProxyMessageDispatcher : IDisposable
    {
        private readonly Func<Exchange, Task> _listener;
        private readonly CancellationTokenSource _haltToken = new CancellationTokenSource();
        private readonly BufferBlock<Exchange> _queue = new BufferBlock<Exchange>();
        private readonly Task _currentTask;
        private bool _disposed;

        public ProxyMessageDispatcher(Func<Exchange, Task> listener)
        {
            _listener = listener;

            if (_listener == null)
                return; 

            _currentTask = Task.Run(Start);
        }

        internal async Task OnNewTask(Exchange exchange)
        {
            if (_listener == null || _disposed)
                return;

            await _queue.SendAsync(exchange).ConfigureAwait(false);
        }


        private async Task Start()
        {
            try
            {
                Exchange current;
                while ((current = await _queue.ReceiveAsync(_haltToken.Token)) != null)
                {
                    await _listener(current);
                }
            }
            catch (OperationCanceledException)
            {
                // Natureal death; 
            }
        }

        public void Dispose()
        {
            _disposed = true;
            _queue.Complete();
            _haltToken.Cancel();
            _currentTask.GetAwaiter().GetResult();
        }
    }
}