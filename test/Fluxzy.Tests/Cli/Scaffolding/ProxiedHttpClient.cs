// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Net;
using System.Net.Http;

namespace Fluxzy.Tests.Cli.Scaffolding
{
    public class ProxiedHttpClient : IDisposable
    {
        private readonly HttpClientHandler _clientHandler;

        public ProxiedHttpClient(int port, string remoteHost = "127.0.0.1")
        {
            _clientHandler = new HttpClientHandler {
                Proxy = new WebProxy($"http://{remoteHost}:{port}"),
                UseProxy = true
            };

            Client = new HttpClient(_clientHandler);
        }

        public HttpClient Client { get; }

        public void Dispose()
        {
            _clientHandler.Dispose();
            Client.Dispose();
        }
    }
}
