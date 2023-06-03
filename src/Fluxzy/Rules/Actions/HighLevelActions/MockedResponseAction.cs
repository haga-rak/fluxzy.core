// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Threading.Tasks;
using Fluxzy.Clients.Mock;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;

namespace Fluxzy.Rules.Actions.HighLevelActions
{
    /// <summary>
    ///     Mock completely a response.
    /// </summary>
    [ActionMetadata("Reply with a pre-made response from a raw text or file")]
    public class MockedResponseAction : Action
    {
        public MockedResponseAction(MockedResponseContent response)
        {
            Response = response;
        }

        /// <summary>
        ///     The response
        /// </summary>
        [ActionDistinctive]
        public MockedResponseContent Response { get; set; }

        public override FilterScope ActionScope => FilterScope.RequestHeaderReceivedFromClient;

        public override string DefaultDescription => "Full response substitution";

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            context.PreMadeResponse = Response;

            return default;
        }

        public static MockedResponseAction BuildDefaultInstance()
        {
            return new MockedResponseAction(new MockedResponseContent(200,
                BodyContent.CreateFromString("Sample content.")));
        }
    }
}
