// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;

namespace Fluxzy.Rules.Actions
{
    [ActionMetadata("An action doing no operation.")]
    public class NoOpAction : Action
    {
        public override FilterScope ActionScope => FilterScope.RequestBodyReceivedFromClient;

        public override string DefaultDescription => "No operation";

        public override string? Description { get; set; } = "No operation";

        public override string FriendlyName => "NoOperation"; 

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            return default; 
        }
    }
}
