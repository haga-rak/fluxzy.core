// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Net;
using System.Text;
using Fluxzy.Core.Socks5;

namespace Fluxzy.Core
{
    /// <summary>
    /// Adapts the existing ProxyAuthenticationMethod for SOCKS5 username/password authentication.
    /// Creates a synthetic RequestHeader to validate credentials using the existing HTTP-based system.
    /// </summary>
    internal class Socks5AuthenticationAdapter
    {
        private readonly ProxyAuthenticationMethod _authMethod;

        public Socks5AuthenticationAdapter(ProxyAuthenticationMethod authMethod)
        {
            _authMethod = authMethod;
        }

        /// <summary>
        /// Returns the SOCKS5 auth method to advertise based on the configured authentication type.
        /// </summary>
        public byte GetSocks5AuthMethod()
        {
            return _authMethod.AuthenticationType switch
            {
                ProxyAuthenticationType.None => Socks5Constants.AuthNoAuth,
                ProxyAuthenticationType.Basic => Socks5Constants.AuthUsernamePassword,
                _ => Socks5Constants.AuthNoAcceptable
            };
        }

        /// <summary>
        /// Returns true if this adapter requires authentication.
        /// </summary>
        public bool RequiresAuthentication => _authMethod.AuthenticationType != ProxyAuthenticationType.None;

        /// <summary>
        /// Validates SOCKS5 username/password credentials by creating a synthetic
        /// RequestHeader with Proxy-Authorization header and delegating to the
        /// existing ProxyAuthenticationMethod.
        /// </summary>
        public bool ValidateCredentials(
            IPEndPoint localEndPoint,
            IPEndPoint remoteEndPoint,
            string username,
            string password)
        {
            if (_authMethod.AuthenticationType == ProxyAuthenticationType.None)
                return true;

            var credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{username}:{password}"));

            var syntheticHeader = CreateSyntheticHeader($"Basic {credentials}");

            return _authMethod.ValidateAuthentication(localEndPoint, remoteEndPoint, syntheticHeader);
        }

        /// <summary>
        /// Selects the best auth method from the client's offered methods.
        /// </summary>
        public byte SelectAuthMethod(byte[] clientMethods)
        {
            var requiredMethod = GetSocks5AuthMethod();

            if (requiredMethod == Socks5Constants.AuthNoAuth)
            {
                return Array.IndexOf(clientMethods, Socks5Constants.AuthNoAuth) >= 0
                    ? Socks5Constants.AuthNoAuth
                    : Socks5Constants.AuthNoAcceptable;
            }

            if (requiredMethod == Socks5Constants.AuthUsernamePassword)
            {
                return Array.IndexOf(clientMethods, Socks5Constants.AuthUsernamePassword) >= 0
                    ? Socks5Constants.AuthUsernamePassword
                    : Socks5Constants.AuthNoAcceptable;
            }

            return Socks5Constants.AuthNoAcceptable;
        }

        private static RequestHeader CreateSyntheticHeader(string proxyAuthValue)
        {
            var headerText =
                "CONNECT socks5.proxy:443 HTTP/1.1\r\n" +
                "Host: socks5.proxy:443\r\n" +
                $"Proxy-Authorization: {proxyAuthValue}\r\n" +
                "\r\n";

            return new RequestHeader(headerText.AsMemory(), true);
        }
    }
}
