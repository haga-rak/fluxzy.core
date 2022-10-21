// Copyright © 2022 Haga Rakotoharivelo

using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Clients.Mock;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules.Actions.HighLevelActions
{
    public class FullResponseAction : Action
    {
        public FullResponseAction(PreMadeResponse preMadeResponse)
        {
            PreMadeResponse = preMadeResponse;
        }

        public PreMadeResponse PreMadeResponse { get; set; }

        public override FilterScope ActionScope => FilterScope.RequestHeaderReceivedFromClient; 

        public override ValueTask Alter(ExchangeContext context, Exchange exchange, Connection connection)
        {
            context.PreMadeResponse = PreMadeResponse;
            return default;
        }

        public override string DefaultDescription => "Full response substitution";
    }
}