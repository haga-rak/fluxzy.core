// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Threading.Tasks;
using Fluxzy.Clients.Dns;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;

namespace Fluxzy.Rules.Actions
{
    /// <summary>
    ///  Use DoH (DNS over HTTPS) to resolve domain names instead of the default DNS provided by the OS.
    /// </summary>
    ///
    [ActionMetadata("Use DoH (DNS over HTTPS) to resolve domain names instead of the default DNS provided by the OS")]
    public class UseDnsOverHttpsAction : Action
    {
        public UseDnsOverHttpsAction(string? nameOrUrl)
        {
            NameOrUrl = nameOrUrl;
        }

        /// <summary>
        /// Name or full Url of the DNS server to use. Built-in values are: Google, Cloudflare.
        /// </summary>

        [ActionDistinctive]
        public string? NameOrUrl { get; set; }

        /// <summary>
        ///  If false, the DNS over HTTPS requests will pass through the proxy.
        /// </summary>
        [ActionDistinctive]
        public bool NoCapture { get; set; } = false; 

        public override FilterScope ActionScope => FilterScope.RequestHeaderReceivedFromClient;

        public override string DefaultDescription => $"DNS over HTTPS";

        public override void Init(StartupContext startupContext)
        {
            base.Init(startupContext);

            if (!string.IsNullOrWhiteSpace(NameOrUrl)) {
                _ = new DnsOverHttpsResolver(NameOrUrl, null); // Validate value
            }
        }

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            if (!string.IsNullOrWhiteSpace(NameOrUrl)) {
                context.DnsOverHttpsNameOrUrl = NameOrUrl;
                context.DnsOverHttpsCapture = !NoCapture;
            }

            return default;
        }

        public override IEnumerable<ActionExample> GetExamples()
        {
            yield return new ActionExample("Use Cloudflare built-in DoH server", new UseDnsOverHttpsAction("CLOUDFLARE"));
            yield return new ActionExample("Use provided DoH server: \"https://dns.google/resolve\". Avoid capturing the DNS requests.", 
                new UseDnsOverHttpsAction("https://dns.google/resolve") {
                    NoCapture = true
                });
        }
    }
}
