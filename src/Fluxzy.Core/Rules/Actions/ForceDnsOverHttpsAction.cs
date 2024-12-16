// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Threading.Tasks;
using Fluxzy.Clients.Dns;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;

namespace Fluxzy.Rules.Actions
{
    /// <summary>
    ///  Use DNS over HTTPs to resolve the domain name.
    /// </summary>
    public class ForceDnsOverHttpsAction : Action
    {
        public ForceDnsOverHttpsAction(string? nameOrUrl)
        {
            NameOrUrl = nameOrUrl;
        }

        /// <summary>
        /// Name or full HTTPS Url of the DNS server to use. Built-in values are: Google, Cloudflare.
        /// </summary>

        [ActionDistinctive]
        public string? NameOrUrl { get; }

        public override FilterScope ActionScope => FilterScope.RequestHeaderReceivedFromClient;

        public override string DefaultDescription => $"DNS over HTTPS";

        public override void Init(StartupContext startupContext)
        {
            base.Init(startupContext);

            if (!string.IsNullOrWhiteSpace(NameOrUrl)) {
                _ = new DnsOverHttpsSolver(NameOrUrl); // Validate value
            }
        }

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            if (!string.IsNullOrWhiteSpace(NameOrUrl)) {
                context.DnsOverHttpsNameOrUrl = NameOrUrl;
            }

            return default;
        }
    }
}
