// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;

namespace Fluxzy.Rules.Actions
{
    /// <summary>
    ///     Force the downstream connection (client-to-proxy) to use HTTP/1.1 by only advertising
    ///     HTTP/1.1 during ALPN negotiation, even when the global ServeH2 option is enabled.
    /// </summary>
    [ActionMetadata(
        "Force the downstream connection (client-to-proxy) to use HTTP/1.1. " +
        "When the global ServeH2 option is enabled, this action overrides it for matched exchanges " +
        "by only advertising HTTP/1.1 during ALPN negotiation with the client.")]
    public class ServeHttp11Action : Action
    {
        public override FilterScope ActionScope => FilterScope.OnAuthorityReceived;

        public override string DefaultDescription => "Serve HTTP/1.1 to client";

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            context.ForceServeHttp11 = true;

            return default;
        }

        public override IEnumerable<ActionExample> GetExamples()
        {
            yield return new ActionExample(
                "Force HTTP/1.1 serving for a specific host even when ServeH2 is globally enabled",
                new ServeHttp11Action());
        }
    }
}
