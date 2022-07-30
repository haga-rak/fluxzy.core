// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Collections;
using Echoes.Clients;

namespace Echoes.Rules.Filters
{
    public abstract class Filter
    {
        public Guid Identifier { get; set; } = Guid.NewGuid();

        public bool Inverted { get; set; }

        protected abstract bool InternalApply(IExchange exchange);

        public abstract FilterScope FilterScope { get; }

        public virtual string FriendlyName { get; } = "Filter" ; 
        
        public virtual bool Apply(IExchange exchange)
        {
            var internalApplyResult = InternalApply(exchange);

            return !Inverted ? internalApplyResult : !internalApplyResult;
        }
    }
}