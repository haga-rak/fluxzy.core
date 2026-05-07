// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Core
{
    /// <summary>
    /// Stable, machine-readable identifiers for the kind of upstream failure that
    /// produced a synthesized 528 response. Emitted as <c>x-fluxzy-network-error</c>
    /// on the response and persisted on <see cref="ClientError.NetworkErrorCode"/>.
    /// </summary>
    public static class NetworkErrorCodes
    {
        // Connection layer
        public const string ConnectionRefused = "connection_refused";
        public const string ConnectionReset = "connection_reset";
        public const string ConnectionAborted = "connection_aborted";
        public const string ConnectionTimeout = "connection_timeout";
        public const string HostUnreachable = "host_unreachable";
        public const string NetworkUnreachable = "network_unreachable";
        public const string ConnectionClosed = "connection_closed";

        // DNS layer
        public const string DnsNotFound = "dns_notfound";
        public const string DnsNoData = "dns_no_data";
        public const string DnsTryAgain = "dns_try_again";
        public const string DnsFailure = "dns_failure";

        // TLS layer
        public const string TlsCertExpired = "tls_cert_expired";
        public const string TlsCertHostnameMismatch = "tls_cert_hostname_mismatch";
        public const string TlsCertUntrusted = "tls_cert_untrusted";
        public const string TlsCertInvalid = "tls_cert_invalid";
        public const string TlsHandshakeFailure = "tls_handshake_failure";

        // Other
        public const string ProtocolError = "protocol_error";
        public const string RuleFailure = "rule_failure";
        public const string Unknown = "unknown";
    }
}
