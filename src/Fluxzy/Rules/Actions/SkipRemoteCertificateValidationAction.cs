// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Core.Breakpoints;

namespace Fluxzy.Rules.Actions
{
    /// <summary>
    ///     Skip validating remote certificate.
    /// </summary>
    [ActionMetadata(
        "Skip validating remote certificate. Fluxzy will ignore any validation errors on the server certificate.")]
    public class SkipRemoteCertificateValidationAction : Action
    {
        public override FilterScope ActionScope => FilterScope.OnAuthorityReceived;

        public override string DefaultDescription => "Skip certificate validation";

        public override ValueTask Alter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            context.SkipRemoteCertificateValidation = true;

            return default;
        }
    }
}
