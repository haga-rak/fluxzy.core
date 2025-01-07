// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Clients.H2;

namespace Fluxzy.Clients.Ssl
{
    public class AdvancedTlsSettings
    {
        /// <summary>
        /// TLS fingerprint settings
        /// </summary>
        public TlsFingerPrint? TlsFingerPrint { get; set; }

        /// <summary>
        /// H2 stream settings
        /// </summary>
        public H2StreamSetting ? H2StreamSetting { get; set; }
    }
}
