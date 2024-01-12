// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Linq;
using Fluxzy.Core.Breakpoints;
using Fluxzy.Rules.Actions;

namespace Fluxzy
{
    internal class ProxyExecutionContext
    {
        public ProxyExecutionContext(FluxzySetting startupSetting)
        {
            BreakPointManager = new BreakPointManager(startupSetting
                                                      .AlterationRules.Where(r => r.Action is BreakPointAction)
                                                      .Select(a => a.Filter));
        }
        
        public BreakPointManager BreakPointManager { get; }
    }
}
