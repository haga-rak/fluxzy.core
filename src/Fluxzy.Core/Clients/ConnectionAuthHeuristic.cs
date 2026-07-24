// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using Fluxzy.Clients.H2.Encoder;
using Fluxzy.Core;

namespace Fluxzy.Clients
{
    /// <summary>
    ///     Detects connection-oriented authentication (NTLM and Negotiate/Kerberos as
    ///     issued by SSPI). Such schemes authenticate the TCP connection rather than the
    ///     request, so an exchange carrying their markers must ride a pinned upstream
    ///     connection and must never let that connection be recycled for another client.
    /// </summary>
    internal static class ConnectionAuthHeuristic
    {
        private static readonly string[] Schemes = { "NTLM", "Negotiate", "Kerberos" };

        public static bool RequestCarriesConnectionAuth(Exchange exchange)
        {
            var header = exchange.Request?.Header;

            if (header == null)
                return false;

            return HasConnectionScheme(header["Authorization"]) ||
                   HasConnectionScheme(header["Proxy-Authorization"]);
        }

        public static bool ResponseCarriesConnectionAuth(Exchange exchange)
        {
            var header = exchange.Response?.Header;

            if (header == null)
                return false;

            return HasConnectionScheme(header["WWW-Authenticate"]) ||
                   HasConnectionScheme(header["Proxy-Authenticate"]);
        }

        public static bool InvolvesConnectionAuth(Exchange exchange)
        {
            return RequestCarriesConnectionAuth(exchange) || ResponseCarriesConnectionAuth(exchange);
        }

        private static bool HasConnectionScheme(IEnumerable<HeaderField> fields)
        {
            foreach (var field in fields) {
                var value = field.Value.Span;

                foreach (var scheme in Schemes) {
                    if (value.StartsWith(scheme.AsSpan(), StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            return false;
        }
    }
}
