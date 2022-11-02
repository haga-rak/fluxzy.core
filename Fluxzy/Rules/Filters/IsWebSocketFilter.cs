// Copyright © 2022 Haga Rakotoharivelo

using System;
using Fluxzy.Misc;

namespace Fluxzy.Rules.Filters
{
    public class IsWebSocketFilter : Filter
    {
        public override Guid Identifier => $"{Inverted}{GetType()}".GetMd5Guid();

        public override FilterScope FilterScope => FilterScope.RequestHeaderReceivedFromClient;

        public virtual string GenericName => "Websocket";

        public override string? ShortName => "ws";

        public override string? Description { get; set; } = "Websocket exchange";

        public override bool PreMadeFilter => true;

        protected override bool InternalApply(IAuthority authority, IExchange? exchange,
            IFilteringContext? filteringContext)
        {
            return exchange?.IsWebSocket ?? false;
        }
    }
}
