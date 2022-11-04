// Copyright © 2022 Haga RAKOTOHARIVELO

using System;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Misc.Converters;
using Fluxzy.Rules.Filters;
using YamlDotNet.Serialization;

namespace Fluxzy.Rules
{
    public abstract class Action : PolymorphicObject
    {
        [YamlIgnore]
        public abstract FilterScope ActionScope { get; }

        [YamlIgnore]
        public int ScopeId => (int) ActionScope;

        public virtual Guid Identifier { get; set; } = Guid.NewGuid();

        [YamlIgnore]
        public abstract string DefaultDescription { get; }

        public virtual string Description { get; set; } = "";

        [YamlIgnore]
        public virtual string FriendlyName =>
            !string.IsNullOrWhiteSpace(Description) ? Description : DefaultDescription;

        public abstract ValueTask Alter(ExchangeContext context, Exchange? exchange, Connection? connection);
    }
}
