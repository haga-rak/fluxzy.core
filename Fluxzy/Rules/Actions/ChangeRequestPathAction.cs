// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules.Actions
{
    /// <summary>
    /// Change request uri path. This action alters only the path of the request.
    /// Please refer to TODO : need an action that redirects the full path 
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

        public override ValueTask Alter(ExchangeContext context, Exchange? exchange, Connection? connection)
        {
            exchange.Request.Header.Path = NewPath.AsMemory();
            return default;
        }

        public override string DefaultDescription => $"Change url path {NewPath}".Trim();
    }
}