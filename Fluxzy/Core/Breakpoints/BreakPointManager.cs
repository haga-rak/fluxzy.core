// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using Fluxzy.Clients;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Core.Breakpoints
{
    public class BreakPointManager
    {
        private readonly Channel<BreakPointContext> _contextQueue = Channel.CreateUnbounded<BreakPointContext>();

        private readonly ConcurrentDictionary<int, BreakPointContext> _runningContext = new();

        public ChannelReader<BreakPointContext> ContextQueue => _contextQueue.Reader;

        public BreakPointContext GetOrCreate(Exchange exchange, FilterScope filterScope)
        {
            lock (_runningContext)
            {
                var context = _runningContext.GetOrAdd(exchange.Id, (_) => new BreakPointContext(exchange, UpdateContext));

                context.CurrentScope = filterScope;

                return context;
            }
        }

        public bool TryGet(int exchangeId, out BreakPointContext? context)
        {
            lock (_runningContext)
                return _runningContext.TryGetValue(exchangeId, out context);
        }

        internal BreakPointContext GetFirst()
        {
            return _runningContext.Values.First();
        }

        private void UpdateContext(BreakPointContext obj)
        {
            // TODO: feed only writer in a debugging context
            _contextQueue.Writer.TryWrite(obj);

            OnContextUpdated?.Invoke(this, new OnContextUpdatedArgs(obj));
        }

        public BreakPointState GetState()
        {
            lock (_runningContext)
                return new BreakPointState(_runningContext.Values.Select(c => c.GetInfo()).ToList());
        }

        public IEnumerable<BreakPointContext> GetAllContext()
        {
            lock (_runningContext)
                return _runningContext.Values.ToList(); 
        }

        public event OnContextUpdated?  OnContextUpdated;
    }

    public delegate void OnContextUpdated(object sender, OnContextUpdatedArgs args);


    public class OnContextUpdatedArgs : EventArgs
    {
        public OnContextUpdatedArgs(BreakPointContext context)
        {
            Context = context;
        }

        public BreakPointContext Context { get;  }
    }

    /// <summary>
    /// The view model of the breakpoint status  
    /// </summary>
    public class BreakPointState
    {
        public BreakPointState(List<BreakPointContextInfo> entries)
        {
            Entries = entries;
        }

        /// <summary>
        /// Define is debugging window has to popup 
        /// </summary>
        public bool HasToPop {
            get
            {
                return Entries.Any(e => e.CurrentHit != null); 
            }
        }

        public List<BreakPointContextInfo> Entries { get; }

        public static BreakPointState EmptyEntries { get; } = new(new());
    }

}
