// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;

namespace Fluxzy.Tests.Cli.Scaffolding
{
    public class ProxiedHttpClient : IDisposable
    {
        private readonly HttpClientHandler _clientHandler;

        public ProxiedHttpClient(
            int port, string remoteHost = "127.0.0.1",
            bool allowAutoRedirect = true, CookieContainer? cookieContainer = null,
            bool automaticDecompression = false, NetworkCredential  ? proxyCredential = null,
            bool useSock5 = false)
        {
            WebProxy = proxyCredential == null
                    ? new WebProxy($"http://{remoteHost}:{port}")
                    : new WebProxy($"http://{remoteHost}:{port}", false, null, proxyCredential)
                ;

            if (useSock5) {

                WebProxy = proxyCredential == null
                    ? new WebProxy($"socks5://{remoteHost}:{port}")
                    : new WebProxy($"socks5://{remoteHost}:{port}", false, null, proxyCredential);
            }

            _clientHandler = new HttpClientHandler {
                Proxy = WebProxy,
                UseProxy = true,
                AllowAutoRedirect = allowAutoRedirect, 
                ServerCertificateCustomValidationCallback = (_, certificate2, arg3, _) => {
                    ServerCertificate = certificate2;
                    ServerChain = arg3;
                    ServerCertificateIssuer = certificate2?.Issuer;
                    return true;
                },
            };

            if (cookieContainer != null)
                _clientHandler.CookieContainer = cookieContainer;

            if (automaticDecompression) {
                _clientHandler.AutomaticDecompression = DecompressionMethods.GZip
                                         | DecompressionMethods.Deflate | DecompressionMethods.Brotli;
            }

            Client = new HttpClient(_clientHandler);
        }

        public WebProxy WebProxy { get; set; }

        public X509Chain? ServerChain { get; private set; }

        public X509Certificate2?  ServerCertificate { get; private set; }

        public string ?  ServerCertificateIssuer { get; private set; }

        public HttpClient Client { get; }

        public void Dispose()
        {
            _clientHandler.Dispose();
            Client.Dispose();
        }
    }
}
