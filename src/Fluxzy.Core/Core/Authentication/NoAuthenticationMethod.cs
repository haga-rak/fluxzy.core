using System;
using System.Net;

namespace Fluxzy.Core
{
    public class NoAuthenticationMethod : ProxyAuthenticationMethod
    {
        public static NoAuthenticationMethod Instance { get; } = new NoAuthenticationMethod();

        private NoAuthenticationMethod()
        {

        }

        public override ProxyAuthenticationType AuthenticationType => ProxyAuthenticationType.None;

        public override bool ValidateAuthentication(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, RequestHeader requestHeader)
        {
            return true;
        }

        public override byte[] GetUnauthorizedResponse(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, RequestHeader requestHeader)
        {
            throw new InvalidOperationException();
        }
    }
}