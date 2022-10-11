// Copyright © 2022 Haga Rakotoharivelo

using Fluxzy.Misc.Converters;
using System;

namespace Fluxzy.Rules.Filters
{
    public abstract class Filter : PolymorphicObject
    {
        public Guid Identifier { get; set; } = Guid.NewGuid();

        public bool Inverted { get; set; }

        protected abstract bool InternalApply(IAuthority authority, IExchange exchange);

        public abstract FilterScope FilterScope { get; }

        public virtual string FriendlyName { get; } = "Filter" ; 
        
        public virtual bool Apply(IAuthority authority, IExchange exchange)
        {
            var internalApplyResult = InternalApply(authority, exchange);

            return !Inverted ? internalApplyResult : !internalApplyResult;
        }

        public bool Locked { get; set; }
    }
}