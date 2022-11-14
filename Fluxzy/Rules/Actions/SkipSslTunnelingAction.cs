// Copyright © 2022 Haga Rakotoharivelo

using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules.Actions
{
    /// <summary>
    /// Tells fluxzy not to decrypt the current traffic. The associated filter  must be on OnAuthorityReceived scope
    /// in order to make this action effective. 
    /// </summary>
    public class SkipSslTunnelingAction : Action
    {
        public override FilterScope ActionScope => FilterScope.OnAuthorityReceived;

        public override ValueTask Alter(ExchangeContext context, Exchange? exchange, Connection? connection)
        {
            context.BlindMode = true;
            return default;
        }

        public override string DefaultDescription => $"Do not decrypt".Trim();
    }
}