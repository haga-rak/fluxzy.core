// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Fluxzy.Clients.Dns
{
    internal class DnsOverHttpsSolver : DefaultDnsSolver
    {
        private readonly Dictionary<string, string> _dnsOverHttpsDefaultUrl = 
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            ["GOOGLE"] = "https://dns.google/query",
            ["CLOUDFLARE"] = "https://dns.google/query",
        };

        private readonly string _finalUrl;
        private readonly HttpClientHandler _clientHandler;
        private readonly HttpClient _client;

        public DnsOverHttpsSolver(string nameOrUrl)
        {
            if (_dnsOverHttpsDefaultUrl.TryGetValue(nameOrUrl, out var url)) {
                _finalUrl = url;
            }
            else {
                if (Uri.TryCreate(nameOrUrl, UriKind.Absolute, out var uriResult) &&
                    uriResult.Scheme.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    _finalUrl = nameOrUrl;
                }
                else {
                    throw new ArgumentException("Invalid DNS over HTTPS URL", nameof(nameOrUrl));
                }
            }

            _clientHandler = new HttpClientHandler();
            _client = new HttpClient(_clientHandler);
        }

        protected override async Task<IPAddress> InternalSolveDns(string hostName, ProxyConfiguration? proxyConfiguration)
        {
            // application/dns-json

            if (proxyConfiguration != null)
            {
                _clientHandler.Proxy = new WebProxy(proxyConfiguration.Host, proxyConfiguration.Port);
            }

            var response = await _client.GetAsync($"{_finalUrl}?name={hostName}&type=A")
                                        .ConfigureAwait(false);



            return default;

        }
    }
}
