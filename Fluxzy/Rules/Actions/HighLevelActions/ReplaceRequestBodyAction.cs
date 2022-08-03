// Copyright © 2022 Haga Rakotoharivelo

using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules.Actions.HighLevelActions
{
    public class ReplaceRequestBodyAction : IAction
    {
        public BodyContent Replacement { get; set; }

        public FilterScope ActionScope => FilterScope.RequestHeaderReceivedFromClient;

        public Task Alter(ExchangeContext context, Exchange exchange, Connection connection)
        {
            throw new System.NotImplementedException();
        }
    }
}