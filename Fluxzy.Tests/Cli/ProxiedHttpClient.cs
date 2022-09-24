// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Net;
using System.Net.Http;

namespace Fluxzy.Tests.Cli
{
    public class ProxiedHttpClient : IDisposable
    {
        private readonly HttpClientHandler _clientHandler;
        private readonly HttpClient _client;

        public ProxiedHttpClient(int port, string remoteHost = "127.0.0.1")
        {
            _clientHandler = new HttpClientHandler()
            {
                Proxy = new WebProxy($"http://{remoteHost}:{port}")
            };

            _client = new HttpClient(_clientHandler); 
        }

        public HttpClient Client => _client;

        public void Dispose()
        {
            _clientHandler.Dispose();
            _client.Dispose();
        }
    }
}