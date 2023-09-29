// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;

namespace Fluxzy.Rules.Actions
{
    /// <summary>
    ///     Instruct fluxzy not to decrypt the current traffic. The associated filter  must be on OnAuthorityReceived scope
    ///     in order to make this action effective.
    /// </summary>
    [ActionMetadata(
        "Instructs fluxzy to not decrypt the current traffic. " +
        "The associated filter  must be on OnAuthorityReceived scope in order to make this action effective. ")]
    public class SkipSslTunnelingAction : Action
    {
        public override FilterScope ActionScope => FilterScope.OnAuthorityReceived;

        public override string DefaultDescription => "Do not decrypt".Trim();

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            context.BlindMode = true;
            return default;
        }
    }
}
