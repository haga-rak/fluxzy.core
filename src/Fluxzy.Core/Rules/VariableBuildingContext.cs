// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using Fluxzy.Core;

namespace Fluxzy.Rules
{
    /// <summary>
    ///    Holds variables datas during an exchange processing
    /// </summary>
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
            LazyVariableEvaluations["exchange.instant"] = () => exchange?.Metrics.ReceivedFromProxy.ToString("O") ?? string.Empty;

            LazyVariableEvaluations["exchange.method"] =
                () => exchange?.Request.Header.Method.ToString() ?? string.Empty;

            LazyVariableEvaluations["exchange.path"] = () => exchange?.Request.Header.Path.ToString() ?? string.Empty;

            LazyVariableEvaluations["exchange.status"] = () =>
                exchange?.StatusCode > 0 ? exchange.StatusCode.ToString() : string.Empty;
        }

        /// <summary>
        ///   Current exchange context
        /// </summary>
        public ExchangeContext ExchangeContext { get; }

        /// <summary>
        ///   The processed exchange if any
        /// </summary>
        public Exchange? Exchange { get; }

        /// <summary>
        ///   The used connection
        /// </summary>
        public Connection? Connection { get; }


        /// <summary>
        ///   Variable evaluations that are lazy evaluated
        /// </summary>
        public IDictionary<string, Func<string>> LazyVariableEvaluations { get; }
    }
}
