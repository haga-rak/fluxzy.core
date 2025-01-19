// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;

namespace Fluxzy.Rules.Actions
{
    /// <summary>
    ///     Affect a tag to exchange. Tags are meta-information and do not alter the connection.
    /// </summary>
    [ActionMetadata("Affect a tag to exchange. Tags are meta-information and do not alter the connection.")]
    public class ApplyTagAction : Action
    {
        public override FilterScope ActionScope => FilterScope.RequestHeaderReceivedFromClient;

        /// <summary>
        ///     Tag value
        /// </summary>
        [ActionDistinctive]
        public Tag? Tag { get; set; }

        public override string DefaultDescription => $"Apply tag {Tag}".Trim();

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            if (Tag != null && exchange != null) {
                exchange.Tags ??= new HashSet<Tag>();
                exchange.Tags.Add(Tag);
            }

            return default;
        }

        public override IEnumerable<ActionExample> GetExamples()
        {
            yield return new ActionExample("Add tag `Hello fluxzy`",
                new ApplyTagAction {
                    Tag = new Tag(Guid.Parse("852D1563-5664-4F17-A4F2-BFE5F7C4993A"), "Hello fluxzy")
                });
        }
    }
}
