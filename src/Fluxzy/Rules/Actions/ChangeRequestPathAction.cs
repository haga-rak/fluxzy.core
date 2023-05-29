// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Core.Breakpoints;

namespace Fluxzy.Rules.Actions
{
    /// <summary>
    ///     Change request uri path. This action alters only the path of the request.
    ///     Please refer to TODO : need an action that redirects the full path
    /// </summary>
    [ActionMetadata(
        "Change request uri path. This action alters only the path of the request. Request path includes query string.")]
    public class ChangeRequestPathAction : Action
    {
        public ChangeRequestPathAction(string newPath)
        {
            NewPath = newPath;
        }

        [ActionDistinctive]
        public string NewPath { get; set; }

        public override FilterScope ActionScope => FilterScope.RequestHeaderReceivedFromClient;

        public override string DefaultDescription => $"Change url path {NewPath}".Trim();

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            if (exchange != null)
                exchange.Request.Header.Path = NewPath.EvaluateVariable(context).AsMemory();

            return default;
        }

        public override IEnumerable<ActionExample> GetExamples()
        {
            yield return new ActionExample("Change request path to `/hello`",
                new ChangeRequestPathAction("/hello"));
        }
    }
}
