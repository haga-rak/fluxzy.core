// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Security.Authentication;
using System.Text.RegularExpressions;

namespace Fluxzy.Readers
{
    public static class SazConnectBodyUtility
    {
        public static SslProtocols ? GetSslVersion(string contentBody)
        {
            var regex = new Regex(@"Version:\s*[0-9\.]+\s*\((?<sslVersion>.*)\)");
            var match = regex.Match(contentBody);

            if (match.Success) {
                var sslVersion = match.Groups["sslVersion"].Value;
                if (sslVersion == "TLS/1.2")
                    return SslProtocols.Tls12;
                if (sslVersion == "TLS/1.3")
                    return SslProtocols.Tls13;
                if (sslVersion == "TLS/1.1")
                    return SslProtocols.Tls11;
                if (sslVersion == "TLS/1")
                    return SslProtocols.Tls;
                if (sslVersion == "SSL/3")
                    return SslProtocols.Ssl3;
                if (sslVersion == "SSL/2")
                    return SslProtocols.Ssl2;
            }

            return null; 
        }
    }
}
