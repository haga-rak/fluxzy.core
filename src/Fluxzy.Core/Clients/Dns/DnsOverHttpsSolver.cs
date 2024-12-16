// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
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

            if (!response.IsSuccessStatusCode) {
                throw new FluxzyException("Failed to resolve DNS over HTTPS");
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            var dnsResponse = JsonSerializer.Deserialize<DnsOverHttpsResponse>(content)!;

            if (dnsResponse.Status != 0)
            {
                throw new FluxzyException($"Failed to resolve DNS over HTTPS. Status response = {dnsResponse.Status}");
            }

            foreach (var answer in dnsResponse.Answer)
            {
                if (answer.Type == 1)
                {
                    return IPAddress.Parse(answer.Data!);
                }
            }

            throw new FluxzyException("Failed to resolve DNS over HTTPS. No A record found");
        }
    }

    public class DnsOverHttpsAnswer
    {
        public DnsOverHttpsAnswer(string name, int? type, string? data)
        {
            Name = name;
            Type = type;
            Data = data;
        }

        [JsonPropertyName("name")]
        public string Name { get; }

        [JsonPropertyName("type")]
        public int? Type { get; }

        [JsonPropertyName("TTL")]
        public int? TTL { get; set; }

        [JsonPropertyName("data")]
        public string? Data { get;  }
    }

    public class DnsOverHttpsQuestion
    {
        public DnsOverHttpsQuestion(string name, int? type)
        {
            Name = name;
            Type = type;
        }

        [JsonPropertyName("name")]
        public string Name { get; }

        [JsonPropertyName("type")]
        public int? Type { get; }
    }

    public class DnsOverHttpsResponse
    {
        [JsonPropertyName("Status")]
        public int? Status { get; set; }

        [JsonPropertyName("TC")]
        public bool? Tc { get; set; }

        [JsonPropertyName("RD")]
        public bool? Rd { get; set; }

        [JsonPropertyName("RA")]
        public bool? Ra { get; set; }

        [JsonPropertyName("AD")]
        public bool? Ad { get; set; }

        [JsonPropertyName("CD")]
        public bool? Cd { get; set; }

        [JsonPropertyName("Question")]
        public List<DnsOverHttpsQuestion> Question { get; set; } = new();

        [JsonPropertyName("Answer")]
        public List<DnsOverHttpsAnswer> Answer { get; set; } = new();

        [JsonPropertyName("Comment")]
        public string? Comment { get; set; }
    }
}
