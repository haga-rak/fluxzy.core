// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using Fluxzy;
using Fluxzy.Clients;
using Fluxzy.Core;
using Fluxzy.Rules;
using Fluxzy.Rules.Filters;
using Fluxzy.Rules.Filters.RequestFilters;
using Fluxzy.Rules.Filters.ResponseFilters;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Rules
{
    public class RuleHeaderFilterFastPathTests
    {
        [Fact]
        public void Header_filters_match_live_exchange_headers()
        {
            var authority = new Authority("api.example.test", 443, true);
            var exchange = CreateExchange(authority);
            var context = new ExchangeContext(authority, new VariableContext(), null, SetUserAgentActionMapping.Default);

            Assert.True(new HasAuthorizationFilter().Apply(context, authority, exchange, null));
            Assert.True(new HasAuthorizationBearerFilter().Apply(context, authority, exchange, null));
            Assert.True(new RequestHeaderFilter("Chrome/120", StringSelectorOperation.Contains, "User-Agent")
                .Apply(context, authority, exchange, null));
            Assert.True(new ResponseHeaderFilter("application/json", StringSelectorOperation.Exact, "Content-Type")
                .Apply(context, authority, exchange, null));
        }

        [Fact]
        public void Header_filters_support_regex_captures_on_live_exchange_headers()
        {
            var authority = new Authority("api.example.test", 443, true);
            var exchange = CreateExchange(authority);
            var context = new ExchangeContext(authority, new VariableContext(), null, SetUserAgentActionMapping.Default);

            Assert.True(new RequestHeaderFilter(@"Chrome/(?<version>\d+)", StringSelectorOperation.Regex, "User-Agent")
                .Apply(context, authority, exchange, null));
            Assert.True(context.VariableContext.TryGet("user.version", out var version));
            Assert.Equal("120", version);
        }

        private static Exchange CreateExchange(Authority authority)
        {
            return Exchange.CreateUntrackedExchange(
                IIdProvider.FromZero,
                new ExchangeContext(authority, new VariableContext(), null, SetUserAgentActionMapping.Default),
                authority,
                (
                    "GET /api/data HTTP/1.1\r\n" +
                    "Host: api.example.test\r\n" +
                    "User-Agent: Mozilla/5.0 benchmark Chrome/120\r\n" +
                    "Authorization: Bearer token\r\n" +
                    "\r\n").AsMemory(),
                Stream.Null,
                (
                    "HTTP/1.1 200 OK\r\n" +
                    "Content-Type: application/json\r\n" +
                    "Content-Length: 0\r\n" +
                    "\r\n").AsMemory(),
                Stream.Null,
                isSecure: true,
                httpVersion: "HTTP/1.1",
                receivedFromProxy: DateTime.UtcNow);
        }
    }
}
