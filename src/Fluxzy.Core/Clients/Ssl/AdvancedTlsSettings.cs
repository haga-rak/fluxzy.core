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


        /// <summary>
        /// Gets or sets a value indicating whether the server certificate should be included in SSL connection
        /// information.
        /// </summary>
        /// <remarks>When enabled, the SSL information will contain the full server certificate details,
        /// which may be useful for diagnostics or auditing. If disabled, certificate details will not be exported in
        /// the SSL info. This setting does not affect the establishment of the SSL connection itself.</remarks>
        public bool ExportCertificateInSslInfo { get; set; }
    }
}
