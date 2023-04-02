// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using Fluxzy.Misc;

namespace Fluxzy.Rules.Filters
{
    /// <summary>
    ///     Select exchanges that are websocket communication
    /// </summary>
    [FilterMetaData(
        LongDescription = "Select websocket exchange."
    )]
    public class IsWebSocketFilter : Filter
    {
        public override Guid Identifier => $"{Inverted}{GetType()}".GetMd5Guid();

        public override FilterScope FilterScope => FilterScope.RequestHeaderReceivedFromClient;

        public override string GenericName => "Websocket";

        public override string? ShortName => "ws";

        public override string? Description { get; set; } = "Websocket exchange";

        public override bool PreMadeFilter => true;

        protected override bool InternalApply(
            IAuthority authority, IExchange? exchange,
            IFilteringContext? filteringContext)
        {
            return exchange?.IsWebSocket ?? false;
        }
    }
}
