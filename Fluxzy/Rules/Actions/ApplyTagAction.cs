// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules.Actions
{
    public class ApplyTagAction : Action
    {
        public override FilterScope ActionScope => FilterScope.OnAuthorityReceived;

        public Tag? Tag { get; set; }

        public override ValueTask Alter(ExchangeContext context, Exchange exchange, Connection connection)
        {
            if (Tag != null)
            {
                exchange.Tags ??= new HashSet<Tag>();
                exchange.Tags.Add(Tag);
            }

            return default;
        }
    }
}