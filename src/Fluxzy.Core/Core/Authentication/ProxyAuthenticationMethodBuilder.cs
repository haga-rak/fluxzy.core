// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Core
{
    public static class ProxyAuthenticationMethodBuilder
    {
        public static ProxyAuthenticationMethod Create(ProxyAuthentication? proxyAuthentication)
        {
            if (proxyAuthentication == null || proxyAuthentication.Type == ProxyAuthenticationType.None)
                return NoAuthenticationMethod.Instance;

            if (proxyAuthentication.Type == ProxyAuthenticationType.Basic)
                return new BasicAuthenticationMethod(proxyAuthentication.Username!, proxyAuthentication.Password!);

            throw new System.NotImplementedException($"Unknown {proxyAuthentication.Type}");
        }
    }
}
