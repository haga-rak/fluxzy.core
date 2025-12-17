// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;

namespace Fluxzy.Rules.Actions.HighLevelActions
{
    /// <summary>
    /// Clears stored session data for a domain or all domains.
    /// </summary>
    [ActionMetadata("Clear stored session data for a specific domain or all domains.")]
    public class ClearSessionAction : Action
    {
        public ClearSessionAction()
        {
        }

        public ClearSessionAction(string? domain)
        {
            Domain = domain;
        }

        /// <summary>
        /// Domain to clear session for. If null or empty, clears all sessions.
        /// Supports variable evaluation.
        /// </summary>
        [ActionDistinctive(Description = "Domain to clear session for (empty for all)")]
        public string? Domain { get; set; }

        public override FilterScope ActionScope => FilterScope.RequestHeaderReceivedFromClient;

        public override string DefaultDescription =>
            string.IsNullOrEmpty(Domain) ? "Clear all sessions" : $"Clear session for {Domain}";

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection,
            FilterScope scope, BreakPointManager breakPointManager)
        {
            var sessionStore = context.VariableContext.SessionStore;
            var domain = Domain?.EvaluateVariable(context);

            if (string.IsNullOrEmpty(domain))
            {
                sessionStore.ClearAll();
            }
            else
            {
                sessionStore.ClearSession(domain);
            }

            return default;
        }

        public override IEnumerable<ActionExample> GetExamples()
        {
            yield return new ActionExample(
                "Clear all stored sessions",
                new ClearSessionAction());

            yield return new ActionExample(
                "Clear session for a specific domain",
                new ClearSessionAction("example.com"));
        }
    }
}
