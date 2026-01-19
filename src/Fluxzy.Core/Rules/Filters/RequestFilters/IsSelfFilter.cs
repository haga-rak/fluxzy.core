// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Net;
using Fluxzy.Core;
using Fluxzy.Misc;

namespace Fluxzy.Rules.Filters.RequestFilters
{
    [FilterMetaData(
        LongDescription = "Check if incoming request considers fluxzy as a web server",
        NotSelectable = true
    )]
    public class IsSelfFilter : Filter
    {
        /// <summary>
        ///     The IP address used by Android emulators to reference the host machine.
        /// </summary>
        internal static readonly IPAddress AndroidEmulatorHostAddress = IPAddress.Parse("10.0.2.2");

        public override FilterScope FilterScope => FilterScope.DnsSolveDone;

        protected override bool InternalApply(
            ExchangeContext? exchangeContext, IAuthority authority, IExchange? exchange, IFilteringContext? filteringContext)
        {
            if (exchangeContext == null || !(exchange is Exchange internalExchange))
                return false;

            if (internalExchange.Metrics.DownStreamLocalPort != exchangeContext.RemoteHostPort)
                return false;

            if (exchangeContext.RemoteHostIp == null)
                return false;

            if (IpUtility.LocalAddresses.Contains(exchangeContext.RemoteHostIp))
                return true;

            // Check for Android emulator host address (10.0.2.2) if enabled
            if (exchangeContext.FluxzySetting?.IncludeAndroidEmulatorHost == true &&
                exchangeContext.RemoteHostIp.Equals(AndroidEmulatorHostAddress)) {
                return true;
            }

            return false;
        }

        public override IEnumerable<FilterExample> GetExamples()
        {
            yield return GetDefaultSample()!;

        }
    }
}
