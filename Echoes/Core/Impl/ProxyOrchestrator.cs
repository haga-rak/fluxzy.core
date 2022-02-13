using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Echoes.Core
{
    internal class ProxyOrchestrator : IDisposable
    {
        private readonly Func<Exchange, Task> _exchangeListener;
        private readonly Func<string, Stream> _throttlePolicy;
        private readonly ProxyStartupSetting _startupSetting;
        private readonly ExchangeBuilder _exchangeBuilder;
        private ProxyMessageDispatcher _dispatcher;
        

        public ProxyOrchestrator(
            Func<Exchange, Task> exchangeListener,
            Func<string, Stream> throttlePolicy,
            ProxyStartupSetting startupSetting,
            ExchangeBuilder exchangeBuilder)
        {
            _exchangeListener = exchangeListener;
            _throttlePolicy = throttlePolicy;
            _startupSetting = startupSetting;
            _exchangeBuilder = exchangeBuilder;
            _dispatcher = new ProxyMessageDispatcher(exchangeListener);
        }

        static int count = 0;

        public async Task Operate(TcpClient client, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    // READ initial state of connection, 
                    var connectionState =
                        await _exchangeBuilder.InitClientConnection(client.GetStream(), _startupSetting, token);

                    if (connectionState == null)
                        return;

                    Exchange exchange =  connectionState.ProvisionalExchange ?? 
                                         ;

                    do
                    {

                    }


                    byte[] buffer = new byte[1024 * 32];

                    var exchange = _exchangeBuilder.ReadExchange(
                        connectionState.Stream,
                        connectionState.Authority,
                        buffer, token
                    ); 




                    var proxyMessage = await _proxyMessageReader.ReadNextMessage(downStreamConnection).ConfigureAwait(false);

                    if (!proxyMessage.Valid)
                    {
                        return ; // We end the loop 
                    }

                    Hpm response = null;
                    IUpstreamClient upstreamClient = null;
                    var shouldCloseUpStreamClient = false; 

                    try
                    {
                        if (proxyMessage.Destination.DestinationType == DestinationType.BlindSecure)
                        {
                            if (await _clientFactory.CreateBlindTunnel(proxyMessage.Destination, downStreamConnection)
                                .ConfigureAwait(false))
                            {
                                // A pipe has been found and created 
                                freeDownStreamConnection = false;
                                return;
                            }
                        }

                        upstreamClient = _clientFactory.GetClientFor(proxyMessage.RequestMessage, proxyMessage.Destination);

                        // MAY throw DNS error 

                        try
                        {
                            await upstreamClient.Init().ConfigureAwait(false);
                        }
                        catch 
                        {
                            connectError = true;
                            throw;
                        }

                        response =
                            await upstreamClient.ProduceResponse(
                                proxyMessage.RequestMessage, 
                                downStreamConnection.WriteStream,
                                _throttlePolicy(proxyMessage.Destination.Host)).ConfigureAwait(false);

                        // If response is an accepted websocket request 
                        if (response != null)
                        {
                            if (response.IsWebSocket)
                            {
                                proxyMessage.RequestMessage.IsWebSocket = true;
                                _clientFactory.CreateWebSocketTunnel(downStreamConnection, upstreamClient.Detach());
                                freeDownStreamConnection = false;

                                return;
                            }

                            if (response.Errors.Any())
                            {
                                shouldCloseUpStreamClient = true;
                                break;
                            }
                        }

                        shouldCloseUpStreamClient = response == null || !response.Valid  || response.ShouldCloseConnection;

                        if (response == null || (response?.CloseDownStreamConnection ?? false) || !response.Valid)
                            break;
                    }

                    catch (EchoesException eex)
                    {
                        proxyMessage.RequestMessage.AddError($"Error while reading server response : {eex.Message}", HttpProxyErrorType.NetworkError, eex.ToString());
                        shouldCloseUpStreamClient = true;
                        freeDownStreamConnection = true;

                        if (connectError)
                        {
                            // Warning client 
                            await ConnectErrorHelper.WriteError(downStreamConnection, eex).ConfigureAwait(false);
                        }

                        throw; 
                    }
                    finally
                    {
                        var upStreamEndPointInfo = upstreamClient?.EndPointInformation;

                        if (upstreamClient != null)
                        {
                            await upstreamClient.Release(shouldCloseUpStreamClient).ConfigureAwait(false);
                        }

                        if (_exchangeListener != null)
                        {
                            var exchange = new HttpExchange(
                                proxyMessage.RequestMessage,
                                response,
                                new EndPointInformation(downStreamConnection),
                                upStreamEndPointInfo);

                            await _dispatcher.OnNewTask(exchange).ConfigureAwait(false);
                        }

                    }
                }
            }
            catch (Exception)
            {
                // Overall exception
                freeDownStreamConnection = true; 
            }
            finally
            {
                if (freeDownStreamConnection || token.IsCancellationRequested)
                {
                    await downStreamConnection.Release(true).ConfigureAwait(false);
                }
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