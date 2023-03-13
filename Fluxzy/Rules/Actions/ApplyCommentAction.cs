// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules.Actions
{
    /// <summary>
    ///     Add comment to exchange. Comment does not alter the stream.
    /// </summary>
    [ActionMetadata("Add comment to exchange. Comment does not alter the stream.")]
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
        public string? Comment { get; set; }

        public override string DefaultDescription => $"Apply comment {Comment}".Trim();

        public override ValueTask Alter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            if (exchange != null)
                exchange.Comment = Comment;

            return default;
        }
    }
}
