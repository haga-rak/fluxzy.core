// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Core;

namespace Fluxzy.Rules
{
    /// <summary>
    ///     Per-exchange bundle of objects that feed built-in <c>${...}</c> variable
    ///     evaluation. Resolved by <see cref="VariableContext.EvaluateVariable"/> via
    ///     <see cref="TryEvaluate"/>; custom variables go through
    ///     <see cref="VariableContext.Set"/> instead of this class.
    /// </summary>
    public class VariableBuildingContext
    {
        private readonly FilterScope _filterScope;

        public VariableBuildingContext(
            ExchangeContext exchangeContext,
            Exchange? exchange, Connection? connection, FilterScope filterScope)
        {
            ExchangeContext = exchangeContext;
            Exchange = exchange;
            Connection = connection;
            _filterScope = filterScope;
        }

        public ExchangeContext ExchangeContext { get; }

        public Exchange? Exchange { get; }

        public Connection? Connection { get; }

        /// <summary>
        ///     Evaluates a built-in variable name (e.g. <c>authority.host</c>,
        ///     <c>exchange.url</c>). Returns <c>false</c> for unknown names — callers
        ///     should then consult <see cref="VariableContext"/> for user-set and
        ///     environment variables.
        /// </summary>
        /// <remarks>
        ///     Allocation-free on the hot path. This replaced an older
        ///     <c>IDictionary&lt;string, Func&lt;string&gt;&gt;</c> surface that
        ///     allocated ~1 KB per rule evaluation (dictionary + nine closures) and
        ///     accounted for 6–12% of bytes in the throughput benchmark.
        /// </remarks>
        public bool TryEvaluate(string name, out string value)
        {
            switch (name) {
                case "authority.host":
                    value = ExchangeContext.Authority.HostName;
                    return true;
                case "authority.port":
                    value = ExchangeContext.Authority.Port.ToString();
                    return true;
                case "authority.secure":
                    value = ExchangeContext.Authority.Secure.ToString();
                    return true;
                case "global.filterScope":
                    value = _filterScope.ToString();
                    return true;
                case "exchange.id":
                    value = Exchange?.Id.ToString() ?? string.Empty;
                    return true;
                case "exchange.url":
                    value = Exchange?.FullUrl ?? string.Empty;
                    return true;
                case "exchange.method":
                    value = Exchange?.Request.Header.Method.ToString() ?? string.Empty;
                    return true;
                case "exchange.path":
                    value = Exchange?.Request.Header.Path.ToString() ?? string.Empty;
                    return true;
                case "exchange.status":
                    value = Exchange?.StatusCode > 0 ? Exchange.StatusCode.ToString() : string.Empty;
                    return true;
                default:
                    value = string.Empty;
                    return false;
            }
        }
    }
}
