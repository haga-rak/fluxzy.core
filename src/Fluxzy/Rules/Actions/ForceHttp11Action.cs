// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Net.Security;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Core.Breakpoints;

namespace Fluxzy.Rules.Actions
{
    /// <summary>
    ///     Force the connection between fluxzy and remote to be HTTP/1.1. This value is enforced by ALPN settings on TLS.
    /// </summary>
    [ActionMetadata(
        "Force the connection between fluxzy and remote to be HTTP/1.1. " +
        "This value is enforced by ALPN settings set during the SSL/Handshake handshake.")]
    public class ForceHttp11Action : Action
    {
        public override FilterScope ActionScope => FilterScope.OnAuthorityReceived;

        public override string DefaultDescription => "Force using HTTP/1.1";

        public override ValueTask Alter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            // TODO avoid allocating new list here 

            context.SslApplicationProtocols = new List<SslApplicationProtocol> {
                SslApplicationProtocol.Http11
            };

            return default;
        }
    }
}
