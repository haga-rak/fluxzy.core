// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Data;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Misc.Converters;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules
{
    public abstract class Action : PolymorphicObject
    {
        public abstract FilterScope ActionScope { get; }
        public int ScopeId => (int) ActionScope;

        public virtual Guid Identifier { get; set; } = Guid.NewGuid();

        public abstract ValueTask Alter(ExchangeContext context, Exchange? exchange, Connection? connection);

        public abstract string DefaultDescription { get; }

        public virtual string Description { get; set; } = "";

        public virtual string FriendlyName => !string.IsNullOrWhiteSpace(Description) ? Description : DefaultDescription;
    }
}