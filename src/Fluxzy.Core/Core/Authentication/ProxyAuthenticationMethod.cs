// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak


// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net;

namespace Fluxzy.Core
{
    public abstract class ProxyAuthenticationMethod
    {
        public abstract ProxyAuthenticationType AuthenticationType { get; }

        public abstract bool ValidateAuthentication(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, RequestHeader requestHeader);

        public abstract byte[] GetUnauthorizedResponse(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, RequestHeader requestHeader);
    }
}
