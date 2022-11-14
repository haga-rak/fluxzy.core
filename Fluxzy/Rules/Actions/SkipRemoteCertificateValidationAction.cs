// Copyright © 2022 Haga RAKOTOHARIVELO

using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules.Actions
{
    /// <summary>
    /// Skip validating remote certicate. 
    /// </summary>
    public class SkipRemoteCertificateValidationAction : Action
    {
        public override FilterScope ActionScope => FilterScope.OnAuthorityReceived;

        public override string DefaultDescription => "Skip certificate validation";

        public override ValueTask Alter(ExchangeContext context, Exchange? exchange, Connection? connection)
        {
            context.SkipRemoteCertificateValidation = true;

            return default; 
        }
    }
}
