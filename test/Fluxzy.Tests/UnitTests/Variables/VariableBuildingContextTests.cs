// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using Fluxzy.Clients;
using Fluxzy.Core;
using Fluxzy.Rules;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Variables
{
    /// <summary>
    ///     Unit-level coverage for <see cref="VariableBuildingContext.TryEvaluate"/>.
    ///     The old implementation exposed the built-ins through an
    ///     <c>IDictionary&lt;string, Func&lt;string&gt;&gt;</c> populated in the constructor;
    ///     these tests lock down the exact value each of the nine built-in names returns
    ///     so the new allocation-free switch must match the prior delegate bodies
    ///     byte-for-byte (including the null-exchange fallback semantics).
    /// </summary>
    public class VariableBuildingContextTests
    {
        private static VariableBuildingContext CreateContext(
            out Exchange exchange,
            string host = "fluxzy.test",
            int port = 8443,
            bool secure = true,
            string requestLine = "POST /some/path?x=1 HTTP/1.1",
            FilterScope filterScope = FilterScope.RequestHeaderReceivedFromClient)
        {
            var authority = new Authority(host, port, secure);
            var setting = FluxzySetting.CreateLocalRandomPort();
            var variableContext = new VariableContext();
            var exchangeContext = new ExchangeContext(authority, variableContext, setting, null!);

            var requestHeader = (requestLine + $"\r\nHost: {host}\r\n\r\n").AsMemory();
            exchange = new Exchange(IIdProvider.FromZero, authority, requestHeader, "HTTP/1.1", DateTime.UtcNow);

            return new VariableBuildingContext(exchangeContext, exchange, connection: null, filterScope);
        }

        [Fact]
        public void TryEvaluate_AuthorityHost_ReturnsHostName()
        {
            var ctx = CreateContext(out _, host: "fluxzy.test");

            Assert.True(ctx.TryEvaluate("authority.host", out var value));
            Assert.Equal("fluxzy.test", value);
        }

        [Fact]
        public void TryEvaluate_AuthorityPort_ReturnsPortAsString()
        {
            var ctx = CreateContext(out _, port: 8443);

            Assert.True(ctx.TryEvaluate("authority.port", out var value));
            Assert.Equal("8443", value);
        }

        [Theory]
        [InlineData(true, "True")]
        [InlineData(false, "False")]
        public void TryEvaluate_AuthoritySecure_ReturnsBoolAsString(bool secure, string expected)
        {
            var ctx = CreateContext(out _, secure: secure);

            Assert.True(ctx.TryEvaluate("authority.secure", out var value));
            Assert.Equal(expected, value);
        }

        [Fact]
        public void TryEvaluate_GlobalFilterScope_ReturnsEnumMemberName()
        {
            var ctx = CreateContext(out _, filterScope: FilterScope.ResponseHeaderReceivedFromRemote);

            Assert.True(ctx.TryEvaluate("global.filterScope", out var value));
            Assert.Equal(nameof(FilterScope.ResponseHeaderReceivedFromRemote), value);
        }

        [Fact]
        public void TryEvaluate_ExchangeId_ReturnsIdAsString()
        {
            var ctx = CreateContext(out var exchange);

            Assert.True(ctx.TryEvaluate("exchange.id", out var value));
            Assert.Equal(exchange.Id.ToString(), value);
        }

        [Fact]
        public void TryEvaluate_ExchangeUrl_ReturnsFullUrl()
        {
            var ctx = CreateContext(out var exchange);

            Assert.True(ctx.TryEvaluate("exchange.url", out var value));
            Assert.Equal(exchange.FullUrl, value);
        }

        [Fact]
        public void TryEvaluate_ExchangeMethod_ReturnsRequestMethod()
        {
            var ctx = CreateContext(out _, requestLine: "POST /x HTTP/1.1");

            Assert.True(ctx.TryEvaluate("exchange.method", out var value));
            Assert.Equal("POST", value);
        }

        [Fact]
        public void TryEvaluate_ExchangePath_ReturnsRequestPath()
        {
            var ctx = CreateContext(out _, requestLine: "GET /some/path?x=1 HTTP/1.1");

            Assert.True(ctx.TryEvaluate("exchange.path", out var value));
            Assert.Equal("/some/path?x=1", value);
        }

        /// <summary>
        ///     Before the response arrives, <c>StatusCode</c> is 0 — the pre-refactor lambda
        ///     returned <see cref="string.Empty"/> (not <c>"0"</c>) for this case, and the
        ///     switch must preserve that.
        /// </summary>
        [Fact]
        public void TryEvaluate_ExchangeStatus_ReturnsEmptyWhenStatusZero()
        {
            var ctx = CreateContext(out var exchange);
            Assert.Equal(0, exchange.StatusCode);

            Assert.True(ctx.TryEvaluate("exchange.status", out var value));
            Assert.Equal(string.Empty, value);
        }

        [Fact]
        public void TryEvaluate_UnknownName_ReturnsFalseAndEmpty()
        {
            var ctx = CreateContext(out _);

            Assert.False(ctx.TryEvaluate("no.such.thing", out var value));
            Assert.Equal(string.Empty, value);
        }

        /// <summary>
        ///     With a null exchange (e.g. pre-request rule evaluation phases), the
        ///     exchange-scoped variables must still return <see cref="string.Empty"/>
        ///     without throwing — matching the <c>exchange?.X ?? string.Empty</c>
        ///     pattern from the pre-refactor lambdas.
        /// </summary>
        [Theory]
        [InlineData("exchange.id")]
        [InlineData("exchange.url")]
        [InlineData("exchange.method")]
        [InlineData("exchange.path")]
        [InlineData("exchange.status")]
        public void TryEvaluate_ExchangeNull_ReturnsEmpty(string variableName)
        {
            var authority = new Authority("fluxzy.test", 443, true);
            var setting = FluxzySetting.CreateLocalRandomPort();
            var variableContext = new VariableContext();
            var exchangeContext = new ExchangeContext(authority, variableContext, setting, null!);

            var ctx = new VariableBuildingContext(
                exchangeContext, exchange: null, connection: null,
                FilterScope.OnAuthorityReceived);

            Assert.True(ctx.TryEvaluate(variableName, out var value));
            Assert.Equal(string.Empty, value);
        }

        /// <summary>
        ///     End-to-end: VariableContext.EvaluateVariable must dispatch through
        ///     <see cref="VariableBuildingContext.TryEvaluate"/> so <c>${authority.host}</c>
        ///     substitutions in rules still resolve.
        /// </summary>
        [Fact]
        public void EvaluateVariable_InterpolatesBuiltInViaBuildingContext()
        {
            var ctx = CreateContext(out _, host: "fluxzy.test", port: 8443);
            var holder = new VariableContext();

            var result = holder.EvaluateVariable("host=${authority.host}:${authority.port}", ctx);

            Assert.Equal("host=fluxzy.test:8443", result);
        }
    }
}
