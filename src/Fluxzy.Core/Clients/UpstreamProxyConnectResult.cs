// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Clients
{
    public enum UpstreamProxyConnectResult
    {
        Ok,
        AuthenticationRequired,
        InvalidStatusCode,
        InvalidResponse = 99,
    }
}
