// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Linq;
using Fluxzy.Core.Breakpoints;
using Fluxzy.Rules.Actions;

namespace Fluxzy
{
    internal class ProxyExecutionContext
    {
        public ProxyExecutionContext(string sessionId, FluxzySetting startupSetting)
        {
            SessionId = sessionId;
            StartupSetting = startupSetting;

            BreakPointManager = new BreakPointManager(startupSetting
                                                      .AlterationRules.Where(r => r.Action is BreakPointAction)
                                                      .Select(a => a.Filter));
        }

        public string SessionId { get; }

        public FluxzySetting StartupSetting { get; }

        public BreakPointManager BreakPointManager { get; }
    }
}
