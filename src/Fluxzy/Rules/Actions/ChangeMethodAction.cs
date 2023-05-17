// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Core.Breakpoints;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules.Actions
{
    /// <summary>
    ///     Change the method of a request.
    /// </summary>
    [ActionMetadata("Alter the method of an exchange.")]
    public class ChangeRequestMethodAction : Action
    {
        public ChangeRequestMethodAction(string newMethod)
        {
            NewMethod = newMethod;
        }

        /// <summary>
        ///     Method name
        /// </summary>
        public string NewMethod { get; set; }

        public override FilterScope ActionScope => FilterScope.RequestHeaderReceivedFromClient;

        public override string DefaultDescription => $"Change method {NewMethod}".Trim();

        public override ValueTask Alter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            if (exchange == null)
                return default;

            exchange.Request.Header.Method = NewMethod.AsMemory();
            

            return default;
        }
    }
}
