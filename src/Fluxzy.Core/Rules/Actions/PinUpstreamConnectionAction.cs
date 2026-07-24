// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;

namespace Fluxzy.Rules.Actions
{
    /// <summary>
    ///     Pins the upstream connection serving the matching exchange to the originating
    ///     downstream connection. The connection is dedicated to that single client and is
    ///     never reused for another, which is what connection-oriented authentication schemes
    ///     (NTLM, Negotiate/Kerberos) require: their handshake and the identity it establishes
    ///     are bound to a single TCP connection. Pinned exchanges always use HTTP/1.1 upstream.
    /// </summary>
    [ActionMetadata(
        "Pin the upstream connection to the originating client connection. " +
        "Required for connection-oriented authentication (NTLM, Negotiate/Kerberos) to work " +
        "through the proxy. Pinned exchanges always use HTTP/1.1 upstream.")]
    public class PinUpstreamConnectionAction : Action
    {
        public override FilterScope ActionScope => FilterScope.RequestHeaderReceivedFromClient;

        public override string DefaultDescription => "Pin upstream connection";

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            context.RequireUpstreamPinning = true;

            return default;
        }
    }
}
