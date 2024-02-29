using System;

namespace Fluxzy.Core
{
    public class NoAuthenticationMethod : ProxyAuthenticationMethod
    {
        public static NoAuthenticationMethod Instance { get; } = new NoAuthenticationMethod();

        private NoAuthenticationMethod()
        {

        }

        public override ProxyAuthenticationType AuthenticationType => ProxyAuthenticationType.None;

        public override bool ValidateAuthentication(
            RequestHeader requestHeader)
        {
            return true;
        }

        public override byte[] GetUnauthorizedResponse(RequestHeader requestHeader)
        {
            throw new InvalidOperationException();
        }
    }
}