// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;

namespace Fluxzy.Rules.Actions
{
    [ActionMetadata("Abort an exchange at the transport level. " +
                    "This action will close connection between " +
                    "fluxzy and client which may lead to depended exchanges to be aborted too.")]
    public class AbortAction : Action
    {
        public override FilterScope ActionScope => FilterScope.RequestHeaderReceivedFromClient;

        public override string DefaultDescription => "Abort exchange";

        public override string? Description { get; set; } = "Abort";

        public override string FriendlyName => "Abort exchange"; 

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            context.Abort = true; 
            return default; 
        }
    }
}
