// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using Fluxzy.Clients;

namespace Fluxzy.Rules
{
    public class VariableBuildingContext
    {
        public VariableBuildingContext(
            ExchangeContext exchangeContext,
            Exchange? exchange, Connection? connection, FilterScope filterScope)
        {
            ExchangeContext = exchangeContext;
            Exchange = exchange;
            Connection = connection;

            LazyVariableEvaluations = new Dictionary<string, Func<string>>();

            LazyVariableEvaluations["authority.host"] = () => ExchangeContext.Authority.HostName;
            LazyVariableEvaluations["authority.port"] = () => ExchangeContext.Authority.Port.ToString();
            LazyVariableEvaluations["authority.secure"] = () => ExchangeContext.Authority.Secure.ToString();

            LazyVariableEvaluations["global.filterScope"] = () => filterScope.ToString();

            LazyVariableEvaluations["exchange.id"] = () => exchange?.Id.ToString() ?? string.Empty;
            LazyVariableEvaluations["exchange.url"] = () => exchange?.FullUrl ?? string.Empty;

            LazyVariableEvaluations["exchange.method"] =
                () => exchange?.Request.Header.Method.ToString() ?? string.Empty;

            LazyVariableEvaluations["exchange.path"] = () => exchange?.Request.Header.Path.ToString() ?? string.Empty;

            LazyVariableEvaluations["exchange.status"] = () =>
                exchange?.StatusCode > 0 ? exchange.StatusCode.ToString() : string.Empty;
        }

        public ExchangeContext ExchangeContext { get; }

        public Exchange? Exchange { get; }

        public Connection? Connection { get; }

        public IDictionary<string, Func<string>> LazyVariableEvaluations { get; }
    }
}
