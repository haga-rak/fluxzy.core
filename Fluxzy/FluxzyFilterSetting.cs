// Copyright © 2022 Haga Rakotoharivelo

using Fluxzy.Rules.Filters;

namespace Fluxzy
{
    public class FluxzyFilterSetting
    {
        /// <summary>
        ///     Filter for skipping decryption
        /// </summary>
        public Filter SkipDecryptionFilter { get; set; } = new NoFilter();

        /// <summary>
        ///     Filter for skipping decryption
        /// </summary>
        public Filter SpoofDnsFilter { get; set; } = new NoFilter();
    }
}