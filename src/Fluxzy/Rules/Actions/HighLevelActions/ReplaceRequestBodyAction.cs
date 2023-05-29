// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Clients.Mock;
using Fluxzy.Core.Breakpoints;

namespace Fluxzy.Rules.Actions.HighLevelActions
{
    /// <summary>
    ///     Replace a request body only
    /// </summary>
    public class ReplaceRequestBodyAction : Action
    {
        public BodyContent? Replacement { get; set; }

        public override FilterScope ActionScope => FilterScope.RequestHeaderReceivedFromClient;

        public override string DefaultDescription => "Full request substitution";

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            throw new NotImplementedException();
        }
    }
}
