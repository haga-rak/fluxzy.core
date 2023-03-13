// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Concurrent;
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
            var context = _runningContext.GetOrAdd(exchange.Id, (_) => new BreakPointContext(exchange, UpdateContext));

            context.CurrentScope = filterScope;

            return context;
        }

        public bool TryGet(int exchangeId, out BreakPointContext? context)
        {
            return _runningContext.TryGetValue(exchangeId, out context);
        }

        internal BreakPointContext GetFirst()
        {
            return _runningContext.Values.First();
        }

        private void UpdateContext(BreakPointContext obj)
        {
            // TODO: Notify UI
            _contextQueue.Writer.TryWrite(obj);
        }
    }

    public class BreakPointContextState
    {
        public FilterScope Scope { get; set; }
    }
}
