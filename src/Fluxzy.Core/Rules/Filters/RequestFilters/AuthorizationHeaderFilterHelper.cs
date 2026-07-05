// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using Fluxzy.Clients.H2.Encoder.Utils;
using Fluxzy.Core;
using Fluxzy.Extensions;

namespace Fluxzy.Rules.Filters.RequestFilters
{
    internal static class AuthorizationHeaderFilterHelper
    {
        private const string AuthorizationHeaderName = "Authorization";

        public static bool HasAuthorization(IExchange? exchange, bool requireBearer)
        {
            if (exchange == null)
                return false;

            if (exchange is Exchange liveExchange)
                return HasLiveAuthorization(liveExchange, requireBearer);

            foreach (var candidate in exchange.GetRequestHeaders().Find(AuthorizationHeaderName)) {
                if (MatchesAuthorizationRequirement(candidate.Value.Span, requireBearer))
                    return true;
            }

            return false;
        }

        private static bool HasLiveAuthorization(Exchange exchange, bool requireBearer)
        {
            foreach (var header in exchange.Request.Header.HeaderFields) {
                if (Http11Constants.IsNonForwardableHeader(header.Name))
                    continue;

                if (!header.Name.Span.Equals(AuthorizationHeaderName, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (MatchesAuthorizationRequirement(header.Value.Span, requireBearer))
                    return true;
            }

            return false;
        }

        private static bool MatchesAuthorizationRequirement(ReadOnlySpan<char> value, bool requireBearer)
        {
            return !requireBearer || value.StartsWith("bearer", StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
