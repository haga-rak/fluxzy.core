// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Threading.Tasks;
using Fluxzy.Clients.Mock;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;

namespace Fluxzy.Rules.Actions.HighLevelActions
{
    /// <summary>
    ///     Reject the request with HTTP 403 Forbidden.
    /// </summary>
    [ActionMetadata(
        "Block the request and return HTTP 403 Forbidden response. " +
        "Use this action to explicitly deny access to specific resources. " +
        "This is a simple blocking action with no configuration required.")]
    public class RejectAction : Action
    {
        public override FilterScope ActionScope => FilterScope.RequestHeaderReceivedFromClient;

        public override string DefaultDescription => "Reject with 403 Forbidden";

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            var bodyContent = Clients.Mock.BodyContent.CreateFromString("Forbidden");
            bodyContent.Type = Clients.Mock.BodyType.Text;

            context.PreMadeResponse = new MockedResponseContent(403, bodyContent);

            return default;
        }

        public override IEnumerable<ActionExample> GetExamples()
        {
            yield return new ActionExample(
                "Block access to a specific domain",
                new RejectAction());
        }
    }
}
