// Copyright © 2022 Haga Rakotoharivelo

using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Misc.Converters;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules
{
    public abstract class Action : PolymorphicObject
    {
        public abstract FilterScope ActionScope { get; }

        public abstract Task Alter(ExchangeContext context, Exchange exchange, Connection connection);
    }
}