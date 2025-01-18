// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;

namespace Fluxzy.Rules.Actions
{
    /// <summary>
    ///     Add comment to exchange. Comment does not alter the stream.
    /// </summary>
    [ActionMetadata("Add comment to exchange. Comment has no effect on the stream behaviour.")]
    public class ApplyCommentAction : Action
    {
        public ApplyCommentAction(string? comment)
        {
            Comment = comment;
        }

        public override FilterScope ActionScope => FilterScope.ResponseHeaderReceivedFromRemote;

        /// <summary>
        ///     Comment
        /// </summary>
        [ActionDistinctive]
        public string? Comment { get; set; }

        public override string DefaultDescription => $"Apply comment {Comment}".Trim();

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            if (exchange != null)
                exchange.Comment = Comment.EvaluateVariable(context);

            return default;
        }

        public override IEnumerable<ActionExample> GetExamples()
        {
            yield return new ActionExample("Add comment `Hello fluxzy`",
                new ApplyCommentAction("Hello fluxzy"));
        }
    }
}
