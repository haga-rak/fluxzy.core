using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Echoes.Core
{
    internal class ProxyOrchestrator : IDisposable
    {
        private readonly ProxyMessageReader _proxyMessageReader;
        private readonly IUpStreamClientFactory _clientFactory;
        private readonly Func<HttpExchange, Task> _exchangeListener;
        private readonly Func<string, Stream> _throttlePolicy;
        private ProxyMessageDispatcher _dispatcher;

        public ProxyOrchestrator(
            ProxyMessageReader proxyMessageReader, 
            IUpStreamClientFactory clientFactory,
            Func<HttpExchange, Task> exchangeListener,
            Func<string, Stream> throttlePolicy)
        {
            _proxyMessageReader = proxyMessageReader;
            _clientFactory = clientFactory;
            _exchangeListener = exchangeListener;
            _throttlePolicy = throttlePolicy;
            _dispatcher = new ProxyMessageDispatcher(exchangeListener);
        }

        static int count = 0;

        public async Task Operate(IDownStreamConnection downStreamConnection, CancellationToken token)
        {
            var freeDownStreamConnection = true;
            var connectError = false; 

            try
            {
                while (!token.IsCancellationRequested)
                {
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
        private readonly Func<HttpExchange, Task> _listener;
        private readonly CancellationTokenSource _haltToken = new CancellationTokenSource();
        private readonly BufferBlock<HttpExchange> _queue = new BufferBlock<HttpExchange>();
        private readonly Task _currentTask;
        private bool _disposed;

        public ProxyMessageDispatcher(Func<HttpExchange, Task> listener)
        {
            _listener = listener;

            if (_listener == null)
                return; 

            _currentTask = Task.Run(Start);
        }

        internal async Task OnNewTask(HttpExchange exchange)
        {
            if (_listener == null || _disposed)
                return;

            await _queue.SendAsync(exchange).ConfigureAwait(false);
        }


        private async Task Start()
        {
            try
            {
                HttpExchange current;
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