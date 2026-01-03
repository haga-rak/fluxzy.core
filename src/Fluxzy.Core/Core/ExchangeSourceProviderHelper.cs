// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Clients;

namespace Fluxzy.Core
{
    internal static class ExchangeSourceProviderHelper
    {
        public static ExchangeSourceProvider GetSourceProvider(FluxzySetting setting,
            SecureConnectionUpdater secureConnectionUpdater, 
            IIdProvider idProvider, ICertificateProvider certificateProvider, 
            ProxyAuthenticationMethod proxyAuthenticationMethod, IExchangeContextBuilder contextBuilder)
        {
            if (!setting.ReverseMode)
                return new ProtocolDetectingSourceProvider(
                    secureConnectionUpdater, idProvider,
                    proxyAuthenticationMethod, contextBuilder);
            
            if (setting.ReverseModePlainHttp)
                return new ReverseProxyPlainExchangeSourceProvider(idProvider, setting.ReverseModeForcedPort, contextBuilder);

            return new ReverseProxyExchangeSourceProvider(certificateProvider, idProvider, setting.ReverseModeForcedPort, contextBuilder);
        }
    }
}
