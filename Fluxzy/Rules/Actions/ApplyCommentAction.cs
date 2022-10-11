// Copyright © 2022 Haga Rakotoharivelo

using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules.Actions
{
    public class ApplyCommentAction : Action
    {
        public override FilterScope ActionScope => FilterScope.OnAuthorityReceived;

        public string? Comment { get; set; }

        public override Task Alter(ExchangeContext context, Exchange exchange, Connection connection)
        {
            exchange.Comment = Comment;
            return Task.CompletedTask; 
        }
    }
}