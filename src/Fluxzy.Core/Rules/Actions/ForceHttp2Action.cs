// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Net.Security;
using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;

namespace Fluxzy.Rules.Actions
{
    /// <summary>
    ///     Force the connection between fluxzy and remote to be HTTP/2.0. This value is enforced by ALPN settings on TLS.
    ///     The exchange will break if the remote does not support HTTP/2.0.
    ///     This action will be ignored when the communication is clear (H2c not supported)
    /// </summary>
    [ActionMetadata(
        "Forces the connection between fluxzy and remote to be HTTP/2.0. This value is enforced when setting up ALPN settings during SSL/TLS negotiation. <br/>" +
        "The exchange will break if the remote does not support HTTP/2.0. <br/>" +
        "This action will be ignored when the communication is clear (h2c not supported).")]
    public class ForceHttp2Action : Action
    {
        private static readonly List<SslApplicationProtocol> Protocols = new() {
            SslApplicationProtocol.Http2
        };

        public override FilterScope ActionScope => FilterScope.OnAuthorityReceived;

        public override string DefaultDescription => "Force using HTTP/2.0";

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            context.SslApplicationProtocols = Protocols;

            return default;
        }
    }
}
