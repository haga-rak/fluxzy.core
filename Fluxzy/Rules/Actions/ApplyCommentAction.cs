// Copyright © 2022 Haga Rakotoharivelo

using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Rules.Filters;
using YamlDotNet.Serialization;

namespace Fluxzy.Rules.Actions
{
    public class ApplyCommentAction : Action
    {
        public ApplyCommentAction(string? comment)
        {
            Comment = comment;
        }

        public override FilterScope ActionScope => FilterScope.ResponseHeaderReceivedFromRemote;

        public string? Comment { get; set; }

        public override ValueTask Alter(ExchangeContext context, Exchange? exchange, Connection? connection)
        {
            if (exchange != null)
                exchange.Comment = Comment;

            return default;
        }

        public override string DefaultDescription => $"Apply comment {Comment}".Trim();
    }
}