// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Clients.Headers;
using Fluxzy.Core.Breakpoints;

namespace Fluxzy.Rules.Actions
{
    /// <summary>
    ///     This action removes all cache directive from request header and response.
    /// </summary>
    [ActionMetadata("Remove all cache directive from request and response headers. This will force the client" +
                    "to ask the latest version of the requested resource.")]
    public class RemoveCacheAction : Action
    {
        public override FilterScope ActionScope => FilterScope.RequestHeaderReceivedFromClient;

        public override string DefaultDescription => "Remove cache";

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            // TODO : reuse the same HeaderAlteration instance here instead of allocating new ones each call

            context.RequestHeaderAlterations.Add(new HeaderAlterationDelete("if-none-match"));
            context.RequestHeaderAlterations.Add(new HeaderAlterationDelete("if-modified-since"));
            context.RequestHeaderAlterations.Add(new HeaderAlterationDelete("etag"));

            context.ResponseHeaderAlterations.Add(new HeaderAlterationDelete("Cache-Control"));
            context.ResponseHeaderAlterations.Add(new HeaderAlterationDelete("Pragma"));
            context.ResponseHeaderAlterations.Add(new HeaderAlterationDelete("Expires"));
            context.ResponseHeaderAlterations.Add(new HeaderAlterationDelete("etag"));
            context.ResponseHeaderAlterations.Add(new HeaderAlterationDelete("last-modified"));

            context.ResponseHeaderAlterations.Add(new HeaderAlterationAdd("Cache-Control",
                "no-cache, no-store, must-revalidate"));

            context.ResponseHeaderAlterations.Add(new HeaderAlterationAdd("Pragma", "no-cache"));
            context.ResponseHeaderAlterations.Add(new HeaderAlterationAdd("Expires", "0"));

            return default;
        }
    }
}
