// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak


// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Core
{
    public abstract class ProxyAuthenticationMethod
    {
        public abstract ProxyAuthenticationType AuthenticationType { get; }

        public abstract bool ValidateAuthentication(RequestHeader requestHeader);

        public abstract byte[] GetUnauthorizedResponse(RequestHeader requestHeader);
    }
}
