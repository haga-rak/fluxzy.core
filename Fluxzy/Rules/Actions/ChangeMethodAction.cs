// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules.Actions
{
    /// <summary>
    /// Change the method of a request.
    /// </summary>
    [ActionMetadata("Alter the method of an exchange.")]
    public class ChangeRequestMethodAction : Action
    {
        public ChangeRequestMethodAction(string newMethod)
        {
            NewMethod = newMethod;
        }

        /// <summary>
        /// Method name
        /// </summary>
        public string NewMethod { get; set; }

        public override FilterScope ActionScope => FilterScope.RequestHeaderReceivedFromClient;

        public override ValueTask Alter(ExchangeContext context, Exchange? exchange, Connection? connection)
        {
            if (exchange == null)
                return default; 

            exchange.Request.Header.Method = NewMethod.AsMemory();

            return default;
        }
        public override string DefaultDescription => $"Change method {NewMethod}".Trim();
    }
}