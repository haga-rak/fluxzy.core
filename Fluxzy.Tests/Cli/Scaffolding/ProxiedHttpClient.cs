// Copyright © 2022 Haga RAKOTOHARIVELO

using System;
using System.Net;
using System.Net.Http;

namespace Fluxzy.Tests.Cli.Scaffolding
{
    public class ProxiedHttpClient : IDisposable
    {
        private readonly HttpClientHandler _clientHandler;

        public HttpClient Client { get; }

        public ProxiedHttpClient(int port, string remoteHost = "127.0.0.1")
        {
            _clientHandler = new HttpClientHandler
            {
                Proxy = new WebProxy($"http://{remoteHost}:{port}")
            };

            Client = new HttpClient(_clientHandler);
        }

        public void Dispose()
        {
            _clientHandler.Dispose();
            Client.Dispose();
        }
    }
}
