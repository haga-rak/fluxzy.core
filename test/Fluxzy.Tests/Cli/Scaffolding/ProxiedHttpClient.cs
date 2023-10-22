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
            int port, string remoteHost = "127.0.0.1", bool allowAutoRedirect = true , CookieContainer? cookieContainer = null)
        {
            _clientHandler = new HttpClientHandler {
                Proxy = new WebProxy($"http://{remoteHost}:{port}"),
                UseProxy = true,
                AllowAutoRedirect = allowAutoRedirect, 
                ServerCertificateCustomValidationCallback = (message, certificate2, arg3, arg4) => {
                    ServerCertificate = certificate2;
                    ServerChain = arg3;
                    return true;
                }
            };

            if (cookieContainer != null)
                _clientHandler.CookieContainer = cookieContainer;

            Client = new HttpClient(_clientHandler);
        }

        public X509Chain? ServerChain { get; private set; }

        public X509Certificate2?  ServerCertificate { get; private set; }

        public HttpClient Client { get; }

        public void Dispose()
        {
            _clientHandler.Dispose();
            Client.Dispose();
        }
    }
}
