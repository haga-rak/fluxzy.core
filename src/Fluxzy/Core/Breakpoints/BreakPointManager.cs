// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Fluxzy.Clients;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Core.Breakpoints
{
    /// <summary>
    ///     Per proxy instance of breakpoint holders
    /// </summary>
    public class BreakPointManager
    {
        private readonly ConcurrentDictionary<int, BreakPointContext> _runningContext = new();
        private readonly List<Filter> _breakPointFilters;

        public BreakPointManager(IEnumerable<Filter> breakPointFilters)
        {
            _breakPointFilters = breakPointFilters.ToList(); 
        }

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

        private void UpdateContext(
            IBreakPointAlterationModel breakPointAlterationModel,
            BreakPointContext breakPointContext)
        {
            OnContextUpdated?.Invoke(this, new OnContextUpdatedArgs(breakPointAlterationModel, breakPointContext));
        }

        public void ClearAllDone()
        {
            lock (_runningContext)
            {
                foreach (var kp in _runningContext.ToList()) {
                    if (kp.Value.GetInfo().Done) {
                        _runningContext.TryRemove(kp.Key, out _);
                    }
                }
            }
        }

        public BreakPointState GetState()
        {
            lock (_runningContext) {
                return new BreakPointState(_runningContext.Values.Select(c => c.GetInfo()).ToList(), _breakPointFilters);
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
        public OnContextUpdatedArgs(IBreakPointAlterationModel breakPointAlterationModel, BreakPointContext context)
        {
            BreakPointAlterationModel = breakPointAlterationModel;
            Context = context;
        }

        public IBreakPointAlterationModel BreakPointAlterationModel { get; }

        public BreakPointContext Context { get; }
    }
}
