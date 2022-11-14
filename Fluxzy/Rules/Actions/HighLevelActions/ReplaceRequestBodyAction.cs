// Copyright © 2022 Haga Rakotoharivelo

using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules.Actions.HighLevelActions
{
    /// <summary>
    /// Replace a request body only 
    /// </summary>
    public class ReplaceRequestBodyAction : Action
    {
        public BodyContent? Replacement { get; set; }

        public override FilterScope ActionScope => FilterScope.RequestHeaderReceivedFromClient;

        public override ValueTask Alter(ExchangeContext context, Exchange? exchange, Connection? connection)
        {
            throw new System.NotImplementedException();
        }

        public override string DefaultDescription => "Full request substitution";
    }
}