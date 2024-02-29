// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net;

namespace Fluxzy.Core
{
    public abstract class ProxyAuthenticationMethod
    {
        public abstract ProxyAuthenticationType AuthenticationType { get; }

        /// <summary>
        /// Validate an authentication request. Return true if the request is authorized, false otherwise.
        /// </summary>
        /// <param name="localEndPoint"></param>
        /// <param name="remoteEndPoint"></param>
        /// <param name="requestHeader"></param>
        /// <returns></returns>
        public abstract bool ValidateAuthentication(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, RequestHeader requestHeader);

        /// <summary>
        /// Returns the raw byte response to send to client in case of unauthorized request. Raw bytes must be a valid HTTP response (starting with HTTP/1.1 and ending with double CRLF).
        /// </summary>
        /// <param name="localEndPoint"></param>
        /// <param name="remoteEndPoint"></param>
        /// <param name="requestHeader"></param>
        /// <returns></returns>
        public abstract byte[] GetUnauthorizedResponse(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, RequestHeader requestHeader);
    }
}
