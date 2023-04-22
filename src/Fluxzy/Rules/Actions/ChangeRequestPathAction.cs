// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Core.Breakpoints;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules.Actions
{
    /// <summary>
    ///     Change request uri path. This action alters only the path of the request.
    ///     Please refer to TODO : need an action that redirects the full path
    /// </summary>
    [ActionMetadata("Change request uri path. This action alters only the path of the request.")]
    public class ChangeRequestPathAction : Action
    {
        public ChangeRequestPathAction(string newPath)
        {
            NewPath = newPath;
        }

        public string NewPath { get; set; }

        public override FilterScope ActionScope => FilterScope.RequestHeaderReceivedFromClient;

        public override string DefaultDescription => $"Change url path {NewPath}".Trim();

        public override ValueTask Alter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            if (exchange != null)
                exchange.Request.Header.Path = NewPath.AsMemory();

            return default;
        }
    }
}
