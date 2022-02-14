using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
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
        private ProxyMessageDispatcher _dispatcher;
        

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
                while (!token.IsCancellationRequested)
                {
                    // READ initial state of connection, 
                    var connectionState =
                        await _exchangeBuilder.InitClientConnection(client.GetStream(), _startupSetting, token);

                    if (connectionState == null)
                        return;

                    Exchange exchange =
                        connectionState.ProvisionalExchange;
                    
                    byte [] buffer = new byte[1024 * 32];
                    
                    do
                    {
                        if (exchange != null && !exchange.Request.Header.Method.Span.Equals("connect", StringComparison.OrdinalIgnoreCase))
                        {
                            var connetionPool = await _poolBuilder.GetPool(exchange, _clientSetting, token);
                            await connetionPool.Send(exchange, token);

                            var intHeaderCount = exchange.Response.Header.WriteHttp11(buffer, true);
                            var headerContent = Encoding.ASCII.GetString(buffer, 0, intHeaderCount);

                            await connectionState.Stream.WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, intHeaderCount),
                                token);

                            if (_exchangeListener != null)
                            {
                                await _exchangeListener(exchange); 
                            }

                            if (exchange.Response.Header.ContentLength != 0 &&
                                exchange.Response.Body != null)
                            {
                                var tc = await exchange.Response.Body.CopyDetailed(
                                    connectionState.Stream, buffer, _ => { }, token);
                            }
                        }

                        exchange = await _exchangeBuilder.ReadExchange(
                            connectionState.Stream,
                            connectionState.Authority,
                            buffer, token
                        );

                    } while (exchange != null); 
                    
                }
            }
            catch (Exception)
            {
                // Overall exception
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