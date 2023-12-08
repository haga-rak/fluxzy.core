// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Fluxzy.Core;
using Fluxzy.Rules.Extensions;

namespace Fluxzy.Rules.Filters.RequestFilters
{
    [FilterMetaData(
        LongDescription =
            "Select exchange according to hostname and a port"
    )]
    public class AuthorityFilter : StringFilter
    {
        [JsonConstructor]
        public AuthorityFilter(int port, string pattern, StringSelectorOperation operation)
            : base(pattern, operation)
        {
            Port = port;
        }

        [FilterDistinctive(Description = "The remote port")]
        public int Port { get; }

        public override FilterScope FilterScope => FilterScope.OnAuthorityReceived;

        public override string? ShortName => "auth.";

        public override string AutoGeneratedName => $"Authority `{Pattern}:{Port}`";

        public override string GenericName => "Filter by authority";

        public override bool Common { get; set; } = true;

        public override string? Description { get; set; } = "Authority (Host and port)";

        protected override IEnumerable<string> GetMatchInputs(
            ExchangeContext? exchangeContext, IAuthority authority, IExchange? exchange)
        {
            var hostName = authority?.HostName ?? exchange?.KnownAuthority;

            if (hostName != null)
                yield return hostName;
        }

        protected override bool InternalApply(
            ExchangeContext? exchangeContext, IAuthority authority, IExchange? exchange,
            IFilteringContext? filteringContext)
        {
            var port = authority?.Port ?? exchange?.KnownPort;

            if (authority?.Port != Port)
                return false;

            return base.InternalApply(exchangeContext, authority, exchange, filteringContext);
        }

        public override IEnumerable<FilterExample> GetExamples()
        {
            yield return new FilterExample("Select only request from host `fluxzy.io` at port 8080",
                new AuthorityFilter(8080, "fluxzy.io", StringSelectorOperation.Exact));

            yield return new FilterExample(
                "Select any exchanges going to a subdomain of `google.com` at port 443",
                new AuthorityFilter(443, "google.com", StringSelectorOperation.EndsWith));
        }
    }

    public static class AuthorityFilterExtensions
    {
        /// <summary>
        /// Chain an AuthorityFilter to a ConfigureFilterBuilder
        /// </summary>
        /// <param name="filterBuilder"></param>
        /// <param name="hostname"></param>
        /// <param name="port"></param>
        /// <param name="operation"></param>
        /// <returns></returns>
        public static IConfigureActionBuilder WhenAuthorityMatch(this IConfigureFilterBuilder filterBuilder,
            string hostname, int port, StringSelectorOperation operation = StringSelectorOperation.Exact)
        {
            return new ConfigureActionBuilder(filterBuilder.FluxzySetting,
                new AuthorityFilter(port, hostname, operation));
        }
    }
}
