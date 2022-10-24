// Copyright © 2022 Haga Rakotoharivelo

using System;
using Fluxzy.Misc;

namespace Fluxzy.Rules.Filters
{
    public class IsWebSocketFilter : Filter
    {
        protected override bool InternalApply(IAuthority authority, IExchange exchange, IFilteringContext? filteringContext)
        {
            return exchange.IsWebSocket; 
        }

        public override Guid Identifier => ($"{Inverted}{GetType()}").GetMd5Guid();

        public override FilterScope FilterScope => FilterScope.RequestHeaderReceivedFromClient;

        public override string GenericName => "Websocket";

        public override string? ShortName => "ws";

        public override string? Description { get; set; } = "Websocket exchange";
    }
}