// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Threading.Tasks;
using Fluxzy.Clients.Headers;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;

namespace Fluxzy.Rules.Actions
{
    /// <summary>
    ///     Add Authorization Bearer token to the request header.
    /// </summary>
    [ActionMetadata("Add Authorization Bearer token to the request header.")]
    public class AddAuthorizationBearerAction : Action
    {
        public AddAuthorizationBearerAction(string token)
        {
            Token = token;
        }

        /// <summary>
        ///    Bearer token
        /// </summary>
        [ActionDistinctive]
        public string Token { get; set; }

        public override FilterScope ActionScope => FilterScope.RequestHeaderReceivedFromClient;

        public override string DefaultDescription => "Add bearer auth.";
        
        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            context.RequestHeaderAlterations.Add(new HeaderAlterationAdd(
                "Authorization",
                $"Bearer {Token}"));

            return default;
        }

        public override IEnumerable<ActionExample> GetExamples()
        {
            yield return new ActionExample("Add Authorization Bearer token to the request header",
                new AddAuthorizationBearerAction("your_token_here"));
        }
    }
}
