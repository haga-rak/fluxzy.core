using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Echoes.Core;

namespace Echoes
{
    internal class ProxyConnectionPool<T> : IDisposable
    {
        private readonly int _concurrentProcessing;
        private readonly Action<T> _todoAction;
        private readonly Channel<T> _blocks = Channel.CreateUnbounded<T>();
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();
        private readonly List<Task> _workingTasks;
        private readonly SemaphoreSlim _initSemaphoreSlim; 

        public ProxyConnectionPool(int concurrentProcessing, Action<T> todoAction)
        {
            _concurrentProcessing = concurrentProcessing;
            _todoAction = todoAction;

            _initSemaphoreSlim = new SemaphoreSlim(0, concurrentProcessing);

            _workingTasks = 
                Enumerable.Range(0 , concurrentProcessing)
                .Select(_ => InnerTask())
                .ToList();
        }

        internal async Task WaitForInit()
        {
            for (int i = 0; i < _concurrentProcessing; i++)
            {
                await _initSemaphoreSlim.WaitAsync().ConfigureAwait(false);
            }

        }

        private async Task InnerTask()
        {
            try
            {
                _initSemaphoreSlim.Release();

                while (await _blocks.Reader.WaitToReadAsync(_tokenSource.Token))
                {
                    if (_blocks.Reader.TryRead(out var result))
                    {
                        _todoAction(result);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Mort naturel
            }
            catch (ObjectDisposedException)
            {

            }
        }

        public void PostWork(T work)
        {
            _blocks.Writer.TryWrite(work);
        }

        public void Dispose()
        {
            _tokenSource.Cancel();
            _blocks.Writer.Complete();
            _initSemaphoreSlim.Dispose();
        }
    }


    internal class ProxyPoolTask
    {
        public ProxyPoolTask(
            long taskId, CancellationToken token, TcpClient tcpClient)
        {
            TaskId = taskId;
            Token = token;
            TcpClient = tcpClient;
        }

        public TcpClient TcpClient { get;  }

       // public IDownStreamConnection Connection { get; set; }

        public long TaskId { get; set; }

        public CancellationToken Token { get; set; }
    }

}