// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Fluxzy.Clients.H2
{
    public class HeaderReceptionScheduler : PipeScheduler, IAsyncDisposable
    {
        public static HeaderReceptionScheduler Default { get; } = new();

        internal class ScheduledItem
        {
            public ScheduledItem(Action<object> action, object arg)
            {
                Action = action;
                Arg = arg;
            }

            public Action<object> Action { get;  }
             
            public object Arg { get; }

        }

        private readonly Channel<ScheduledItem> _channel 
            = Channel.CreateUnbounded<ScheduledItem>();
        private readonly Task _task;
        private SemaphoreSlim Sync { get; } = new SemaphoreSlim(1);

        public HeaderReceptionScheduler()
        {
            _task = Run();
        }

        public async Task Run()
        {
            while (await _channel.Reader.WaitToReadAsync())
            {
                if (_channel.Reader.TryRead(out var item))
                {
                    item.Action(item.Arg);
                    Sync.Release();
                }
            }
        }

        public override void Schedule(Action<object> action, object state)
        {
            Sync.Wait();
            _channel.Writer.TryWrite(new ScheduledItem(action, state)); 
        }
        

        public async ValueTask DisposeAsync()
        {
            _channel.Writer.Complete();
            await _task; 
        }
    }
}