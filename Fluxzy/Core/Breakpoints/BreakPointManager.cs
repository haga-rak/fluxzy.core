// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Core.Breakpoints
{
    public class BreakPointManager
    {
        public BreakPointManager()
        {

        }

        private readonly Channel<BreakPointContext> _contextQueue = Channel.CreateUnbounded<BreakPointContext>();

        private readonly ConcurrentDictionary<int, BreakPointContext> _runningContext = new();

        public ChannelReader<BreakPointContext> ContextQueue => _contextQueue.Reader;

        public BreakPointContext GetOrCreate(Exchange exchange, Filter filter, FilterScope filterScope)
        {
            lock (_runningContext) {
                var context =
                    _runningContext.GetOrAdd(exchange.Id, _ => new BreakPointContext(exchange, filter, UpdateContext));

                context.CurrentScope = filterScope;

                return context;
            }
        }

        public bool TryGet(int exchangeId, out BreakPointContext? context)
        {
            lock (_runningContext) {
                return _runningContext.TryGetValue(exchangeId, out context);
            }
        }

        internal BreakPointContext GetFirst()
        {
            lock (_runningContext) {
                return _runningContext.Values.First();
            }
        }

        private void UpdateContext(BreakPointContext breakPointContext)
        {
            // TODO: feed only writer in a debugging context

            if (Environment.GetEnvironmentVariable("TEST_CONTEXT") == "true")
                _contextQueue.Writer.TryWrite(breakPointContext);

            Task.Run(async () => {
                await Task.Delay(50); // TODO : Find a better trick than this. The main issue is that this event is trigger earlier 
                // compared to the availability of BreakPointContext from UiState point of view
                OnContextUpdated?.Invoke(this, new OnContextUpdatedArgs(breakPointContext));
            }); 
        }

        public BreakPointState GetState()
        {
            lock (_runningContext) {
                return new BreakPointState(_runningContext.Values.Select(c => c.GetInfo()).ToList());
            }
        }

        public IEnumerable<BreakPointContext> GetAllContext()
        {
            lock (_runningContext) {
                return _runningContext.Values.ToList();
            }
        }

        public event OnContextUpdated? OnContextUpdated;
    }

    public delegate void OnContextUpdated(object sender, OnContextUpdatedArgs args);

    public class OnContextUpdatedArgs : EventArgs
    {
        public OnContextUpdatedArgs(BreakPointContext context)
        {
            Context = context;
        }

        public BreakPointContext Context { get; }
    }
}
