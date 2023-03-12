// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Fluxzy.Clients;

namespace Fluxzy.Rules.Actions
{
    public class BreakPointManager
    {
        private ConcurrentDictionary<int, BreakPointContext> _runningContext = new(); 

        private readonly Action<BreakPointState> _breakPointStateListener;

        public BreakPointManager(Action<BreakPointState> breakPointStateListener)
        {
            _breakPointStateListener = breakPointStateListener;
        }

        public BreakPointContext GetOrCreate(int exchangeId)
        {
            return _runningContext.GetOrAdd(exchangeId, (eId) => new BreakPointContext(exchangeId)); 
        }


    }

    public class BreakPointState
    {

    }
}
