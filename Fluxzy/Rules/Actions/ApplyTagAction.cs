// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules.Actions
{
    /// <summary>
    ///     Affect a tag to exchange. Tags are meta-information and do not alter the connection.
    /// </summary>
    [ActionMetadata("Affect a tag to exchange. Tags are meta-information and do not alter the connection.")]
    public class ApplyTagAction : Action
    {
        public override FilterScope ActionScope => FilterScope.OnAuthorityReceived;

        /// <summary>
        ///     Tag value
        /// </summary>
        public Tag? Tag { get; set; }

        public override string DefaultDescription => $"Apply tag {Tag}".Trim();

        public override ValueTask Alter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            if (Tag != null && exchange != null) {
                exchange.Tags ??= new HashSet<Tag>();
                exchange.Tags.Add(Tag);
            }

            return default;
        }
    }
}
