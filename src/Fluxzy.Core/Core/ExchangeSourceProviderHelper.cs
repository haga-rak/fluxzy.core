// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Clients;

namespace Fluxzy.Core
{
    internal static class ExchangeSourceProviderHelper
    {
        public static ExchangeSourceProvider GetSourceProvider(FluxzySetting setting,
            SecureConnectionUpdater secureConnectionUpdater, 
            IIdProvider idProvider, ICertificateProvider? certificateProvider, 
            ProxyAuthenticationMethod proxyAuthenticationMethod)
        {
            if (!setting.ReverseMode)
                return new FromProxyConnectSourceProvider(
                    secureConnectionUpdater, idProvider,
                    proxyAuthenticationMethod);
            
            if (setting.ReverseModePlainHttp)
                return new ReverseProxyPlainExchangeSourceProvider(idProvider, setting.ReverseModeForcedPort);

            return new ReverseProxyExchangeSourceProvider(certificateProvider, idProvider, setting.ReverseModeForcedPort);
        }
    }
}
