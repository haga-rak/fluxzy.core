// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Security.Authentication;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Core.Breakpoints;

namespace Fluxzy.Rules.Actions
{
    /// <summary>
    ///     Force the usage of a specific TLS version. Values can be chosen among : Tls, Tls11, Tls12, Tls13 (if .NET 6+),
    ///     Ssl3, Ssl2.
    ///     Forcing the usage of a specific TLS version can break the exchange if the remote does not support the requested
    ///     protocol.
    /// </summary>
    [ActionMetadata(
        "Force the usage of a specific TLS version. Values can be chosen among : Tls, Tls11, Tls12, Tls13, Ssl3, Ssl2. <br/>" +
        "Forcing the usage of a specific TLS version can break the exchange if the remote does not support the requested protocol.")]
    public class ForceTlsVersionAction : Action
    {
        public ForceTlsVersionAction(SslProtocols sslProtocols)
        {
            SslProtocols = sslProtocols;
        }

        /// <summary>
        ///     SslProtocols : Values can be chosen among : Tls, Tls11, Tls12, Tls13 (if .NET 6+), Ssl3, Ssl2.
        /// </summary>
        [ActionDistinctive]
        public SslProtocols SslProtocols { get; set; }

        public override FilterScope ActionScope => FilterScope.RequestHeaderReceivedFromClient;

        public override string DefaultDescription => $"Force using {SslProtocols}";

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            context.ProxyTlsProtocols = SslProtocols;

            return default;
        }

        public override IEnumerable<ActionExample> GetExamples()
        {
            yield return new ActionExample("Accept only TLS 1.1 connections.",
#pragma warning disable SYSLIB0039
                new ForceTlsVersionAction(SslProtocols.Tls11));
#pragma warning restore SYSLIB0039
        }
    }
}
