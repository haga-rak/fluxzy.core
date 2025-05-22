// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Utils;

namespace Fluxzy.Clients.Dns
{
    internal class DnsOverHttpsResolver : DefaultDnsResolver
    {
        private static readonly Dictionary<string, string> DnsOverHttpsDefaultUrl = new(StringComparer.OrdinalIgnoreCase) {
            ["GOOGLE"] = Environment.GetEnvironmentVariable("FLUXZY_DEFAULT_GOOGLE_DNS_URL") ?? "https://8.8.8.8/resolve",
            ["CLOUDFLARE"] = Environment.GetEnvironmentVariable("FLUXZY_DEFAULT_CLOUDFLARE_DNS_URL") ?? "https://1.1.1.1/dns-query",
        };

        private static readonly HashSet<string> ByPassList = new(new[] { "localhost" }, StringComparer.OrdinalIgnoreCase);

        private static readonly HttpMessageHandler DefaultHandler = new HttpClientHandler() {
            UseProxy = false,
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };

        private readonly ConcurrentDictionary<DnsCacheKey, IReadOnlyCollection<string?>> _cache = new();

        private readonly string _finalUrl;
        private readonly HttpClient _client;
        private readonly string _host;

        public DnsOverHttpsResolver(string nameOrUrl, ProxyConfiguration? proxyConfiguration)
        {
            if (DnsOverHttpsDefaultUrl.TryGetValue(nameOrUrl, out var url)) {
                _finalUrl = url;
            }
            else {
                if (Uri.TryCreate(nameOrUrl, UriKind.Absolute, out var uriResult) &&
                    uriResult.Scheme.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    _finalUrl = nameOrUrl.TrimEnd('?');
                }
                else {
                    throw new ArgumentException("Invalid DNS over HTTPS URL", nameof(nameOrUrl));
                }
            }

            if (!Uri.TryCreate(_finalUrl, UriKind.Absolute, out var finalUrl)) {
                throw new ArgumentException($"FinalUrl must be absolute : {finalUrl}"); 
            }

            _host = finalUrl.Host;

            _client = new HttpClient(proxyConfiguration?.GetDefaultHandler() ?? DefaultHandler);
        }

        protected async Task<IReadOnlyCollection<string?>> GetDnsData(string type, string hostName)
        {
            var key = new DnsCacheKey(type, hostName);

            using var _ = await Synchronizer<DnsCacheKey>.Shared.LockAsync(key).ConfigureAwait(false);

            if (_cache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var result = await InternalGetDnsData(type, hostName);

            _cache[key] = result;

            return result;
        }

        private async Task<List<string?>> InternalGetDnsData(string type, string hostName)
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{_finalUrl}?name={hostName}&type={type}");

            requestMessage.Headers.Add("Accept", "application/dns-json");

            using var response = await _client.SendAsync(requestMessage, HttpCompletionOption.ResponseContentRead)
                                              .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new FluxzyException("Failed to resolve DNS over HTTPS");
            }

            await using var content = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

            var dnsResponse = await JsonSerializer.DeserializeAsync<DnsOverHttpsResponse>(content).ConfigureAwait(false);

            if (dnsResponse == null)
            {
                throw new ClientErrorException(0, "Invalid DNS response (null)");
            }

            if (dnsResponse.Status != 0)
            {
                throw new ClientErrorException(0,
                    $"Failed to resolve DNS over HTTPS. Status response = {dnsResponse.Status}");
            }

            var result = dnsResponse.Answers
                                    .Where(a => a.Type == 1)
                                    .Select(a => a.Data)
                                    .ToList();

            return result;
        }

        protected override async Task<IEnumerable<IPAddress>> InternalSolveDns(string hostName)
        {
            if (string.Equals(hostName, _host, StringComparison.OrdinalIgnoreCase) 
                || ByPassList.Contains(hostName) 
                || IPAddress.TryParse(hostName, out _)
                ) {
                // fallback to default resolver to avoid resolving loop
                return await base.InternalSolveDns(hostName).ConfigureAwait(false); 
            }

            // application/dns-json

            var values = await GetDnsData("A", hostName).ConfigureAwait(false);

            // reply with a single linq request 

            var result = values
                   .Select(a => IPAddress.TryParse(a, out var ip) ? ip : default)
                   .Where(ip => ip != null).OfType<IPAddress>()
                   .ToList();

            if (!result.Any())
            {
                return await base.InternalSolveDns(hostName).ConfigureAwait(false);
            }

            return result;
        }
    }

    internal record struct DnsCacheKey
    {
        public readonly bool Equals(DnsCacheKey other)
        {
            return Type == other.Type && Host == other.Host;
        }

        public readonly override int GetHashCode()
        {
            return HashCode.Combine(Type, Host);
        }

        public DnsCacheKey(string type, string host)
        {
            Type = type;
            Host = host;
        }
        
        public string Type { get; } 

        public string Host { get; }
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
        public List<DnsOverHttpsAnswer> Answers { get; set; } = new();

        [JsonPropertyName("Comment")]
        public string? Comment { get; set; }
    }
}
