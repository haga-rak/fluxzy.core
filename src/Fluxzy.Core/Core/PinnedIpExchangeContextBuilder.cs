// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net;
using System.Threading.Tasks;

namespace Fluxzy.Core
{
    /// <summary>
    ///     Decorates an exchange context builder so every context created for a connection keeps
    ///     targeting the IP the client originally connected to (SNI host recovery). The pin applies
    ///     only when no rule already forced a remote IP.
    /// </summary>
    internal sealed class PinnedIpExchangeContextBuilder : IExchangeContextBuilder
    {
        private readonly IExchangeContextBuilder _inner;
        private readonly IPAddress _pinnedIp;

        public PinnedIpExchangeContextBuilder(IExchangeContextBuilder inner, IPAddress pinnedIp)
        {
            _inner = inner;
            _pinnedIp = pinnedIp;
        }

        public async ValueTask<ExchangeContext> Create(Authority authority, bool secure)
        {
            var context = await _inner.Create(authority, secure).ConfigureAwait(false);

            context.RemoteHostIp ??= _pinnedIp;

            return context;
        }
    }
}
