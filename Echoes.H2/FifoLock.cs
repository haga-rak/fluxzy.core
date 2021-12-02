// Copyright © 2021 Haga Rakotoharivelo

using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Echoes.H2
{
    internal class FifoLock : IAsyncDisposable
    {
        private readonly Channel<Action> _queueChannel = Channel.CreateUnbounded<Action>(
            new UnboundedChannelOptions()
            {
                SingleReader = true, 
                SingleWriter = true
            });

        private readonly CancellationTokenSource _endingSource = new CancellationTokenSource(); 

        private readonly Task _innerTask;

        public FifoLock()
        {
            _innerTask = InnerTask();
        }

        public async Task InnerTask()
        {
            try
            {
                while (!_endingSource.IsCancellationRequested && await _queueChannel.Reader.WaitToReadAsync(_endingSource.Token))
                {
                    if (_endingSource.IsCancellationRequested)
                        break;

                    if (_queueChannel.Reader.TryRead(out var todo))
                        todo();
                }
            }
            catch (OperationCanceledException)
            {
                // Cancel request end loop 
            }
        }


        public void Enqueue(Action todo)
        {
            // single writer always return true
            _queueChannel.Writer.TryWrite(todo); 
        }

        public async ValueTask DisposeAsync()
        {
            _endingSource.Cancel();
            await _innerTask.ConfigureAwait(false);  // Wait for task to completly end
        }
    }
}