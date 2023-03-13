// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Fluxzy.Clients;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules.Actions
{
    public class BreakPointManager
    {
        private readonly ConcurrentDictionary<int, BreakPointContext> _runningContext = new();

        public BreakPointManager(Action<BreakPointState> breakPointStateListener)
        {

        }

        public BreakPointContext GetOrCreate(Exchange exchange, FilterScope filterScope)
        {
            var context =  _runningContext.GetOrAdd(exchange.Id, (_) => new BreakPointContext(exchange, UpdateContext));

            context.CurrentScope = filterScope;

            return context; 
        }

        private void UpdateContext(BreakPointContext obj)
        {
            // TODO: Notify UI
        }
    }

    public class BreakPointState
    {
        public List<BreakPointContextState> States { get; set; } = new();
    }

    public class BreakPointContextState
    {
        public FilterScope Scope { get; set; }
    }
}
