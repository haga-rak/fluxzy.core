// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;

namespace Fluxzy.Rules.Actions
{
    /// <summary>
    ///    Instruct fluxzy to use an upstream proxy.
    /// </summary>
    [ActionMetadata("Use an upstream proxy.", NonDesktopAction = true)]
    public class UpStreamProxyAction : Action
    {
        public UpStreamProxyAction(string host, int port)
        {
            Host = host;
            Port = port;
        }

        [ActionDistinctive]
        public string Host { get; set; }

        [ActionDistinctive]
        public int Port { get; set; }

        [ActionDistinctive]
        public string? ProxyAuthorizationHeader { get; set; }

        /// <summary>
        ///     Hosts that must bypass the upstream proxy and connect directly. Each entry matches the
        ///     request hostname (port excluded) as follows: <c>*</c> bypasses every host, a bare entry
        ///     such as <c>example.com</c> matches that host and any of its subdomains, and an explicit
        ///     <c>*.example.com</c> (or <c>.example.com</c>) matches the domain and its subdomains.
        ///     Matching is case-insensitive.
        /// </summary>
        [ActionDistinctive(Description = "Hosts that bypass the upstream proxy and connect directly")]
        public List<string> ByPassHosts { get; set; } = new();

        public override FilterScope ActionScope => FilterScope.OnAuthorityReceived;

        public override string DefaultDescription => $"Upstream proxy to {Host}:{Port}";

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            if (Host != null! && Port != 0 && !IsByPassed(ByPassHosts, context.Authority?.HostName))
                context.ProxyConfiguration = new ProxyConfiguration(Host, Port, ProxyAuthorizationHeader);

            return default;
        }

        /// <summary>
        ///     True when <paramref name="hostName" /> matches any entry of <paramref name="byPassHosts" />.
        ///     See <see cref="ByPassHosts" /> for the matching rules.
        /// </summary>
        internal static bool IsByPassed(IReadOnlyCollection<string>? byPassHosts, string? hostName)
        {
            if (hostName == null || byPassHosts == null || byPassHosts.Count == 0)
                return false;

            foreach (var rawEntry in byPassHosts) {
                if (string.IsNullOrWhiteSpace(rawEntry))
                    continue;

                var entry = rawEntry.Trim();

                if (entry == "*")
                    return true;

                // Normalize the wildcard form "*.example.com" to a suffix ".example.com".
                if (entry.StartsWith("*.", StringComparison.Ordinal))
                    entry = entry.Substring(1);

                if (entry.StartsWith(".", StringComparison.Ordinal)) {
                    var bare = entry.Substring(1);

                    if (string.Equals(hostName, bare, StringComparison.OrdinalIgnoreCase)
                        || hostName.EndsWith(entry, StringComparison.OrdinalIgnoreCase))
                        return true;

                    continue;
                }

                // Bare host or domain: matches the host itself or any of its subdomains.
                if (string.Equals(hostName, entry, StringComparison.OrdinalIgnoreCase)
                    || hostName.EndsWith("." + entry, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public override IEnumerable<ActionExample> GetExamples()
        {
            yield return new ActionExample("Use an upstream proxy to 192.168.1.9 on port 8080",
                new UpStreamProxyAction("192.168.1.9", 8080));

            yield return new ActionExample("Use an upstream proxy to 192.168.1.9 on port 8080 with basic auth" +
                                           " login: leeloo , password: multipass",
                new UpStreamProxyAction("192.168.1.9", 8080) {
                    ProxyAuthorizationHeader = "Basic bGVlbG9vOm11bHRpcGFzcw=="
                });

            yield return new ActionExample("Use an upstream proxy to 192.168.1.9 on port 8080 but connect" +
                                           " directly to localhost and any *.internal.lan host",
                new UpStreamProxyAction("192.168.1.9", 8080) {
                    ByPassHosts = { "localhost", "*.internal.lan" }
                });
        }
    }
}
