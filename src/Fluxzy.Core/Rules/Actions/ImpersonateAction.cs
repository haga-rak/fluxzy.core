// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;

namespace Fluxzy.Rules.Actions
{
    /// <summary>
    /// Impersonate a browser or client
    /// </summary>
    [ActionMetadata("Impersonate a browser or client")]
    public class ImpersonateAction : Action
    {
        public ImpersonateAction(string nameOrConfigFile)
        {
            NameOrConfigFile = nameOrConfigFile;
        }

        /// <summary>
        /// Name or config file
        /// </summary>
        [ActionDistinctive]
        public string NameOrConfigFile { get; }

        public override FilterScope ActionScope => FilterScope.RequestHeaderReceivedFromClient;

        public override string DefaultDescription { get; } = "Impersonate";

        public override void Init(StartupContext startupContext)
        {
            base.Init(startupContext);
        }

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            throw new System.NotImplementedException();
        }
    }
}
