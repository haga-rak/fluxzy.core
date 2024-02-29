using System;
using System.Linq;
using System.Net;
using System.Text;

namespace Fluxzy.Core
{
    public class BasicAuthenticationMethod : ProxyAuthenticationMethod
    {
        private static readonly byte[] ProxyAuthenticationRequiredRawData =
            Encoding.UTF8.GetBytes("HTTP/1.1 407 Proxy Authentication Required\r\nProxy-Authenticate: Basic realm=\"Fluxzy\"\r\n\r\n");

        private readonly string _base64Header;

        public BasicAuthenticationMethod(string username, string password)
        {
            _base64Header = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        }

        public override ProxyAuthenticationType AuthenticationType => ProxyAuthenticationType.Basic;

        public override bool ValidateAuthentication(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, RequestHeader requestHeader)
        {
            var authorizationHeader = requestHeader["Proxy-Authorization"]?.FirstOrDefault();

            if (authorizationHeader == null)
                return false;

            if (!authorizationHeader.Value.Value.Span.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
                return false;

            var base64 = authorizationHeader.Value.Value.Span.Slice(6);

            return base64.Equals(_base64Header, StringComparison.Ordinal);
        }

        public override byte[] GetUnauthorizedResponse(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, RequestHeader requestHeader)
        {
            return ProxyAuthenticationRequiredRawData;
        }
    }
}