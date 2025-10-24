// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Threading.Tasks;
using Fluxzy.Clients.Headers;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;

namespace Fluxzy.Rules.Actions
{
    /// <summary>
    ///      Add Authorization Basic to the request header.
    /// </summary>
    [ActionMetadata("Add Authorization Basic to the request header.")]
    public class AddAuthorizationBasicAction : Action
    {
        public AddAuthorizationBasicAction(string user, string password)
        {
            User = user;
            Password = password;
        }

        /// <summary>
        ///    Password
        /// </summary>
        [ActionDistinctive]
        public string Password { get; set; }

        /// <summary>
        ///    User
        /// </summary>
        [ActionDistinctive]
        public string User { get; set; }

        public override FilterScope ActionScope => FilterScope.RequestHeaderReceivedFromClient;

        public override string DefaultDescription => "Add basic auth.";

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            var plainText = $"{User.EvaluateVariable(context)}:{Password.EvaluateVariable(context)}";
            var base64 = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(plainText));
            context.RequestHeaderAlterations.Add(new HeaderAlterationAdd(
                "Authorization",
                $"Basic {base64}"));
            return default;
        }

        public override IEnumerable<ActionExample> GetExamples()
        {
            yield return new ActionExample("Add Authorization Basic to the request header",
                new AddAuthorizationBasicAction("username", "password"));
        }
    }
}
