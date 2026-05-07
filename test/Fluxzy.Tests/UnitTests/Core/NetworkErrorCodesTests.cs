// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Core;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Core
{
    public class NetworkErrorCodesTests
    {
        [Theory]
        [InlineData("connection_refused", NetworkErrorCodes.ConnectionRefused)]
        [InlineData("connection_reset", NetworkErrorCodes.ConnectionReset)]
        [InlineData("connection_aborted", NetworkErrorCodes.ConnectionAborted)]
        [InlineData("connection_timeout", NetworkErrorCodes.ConnectionTimeout)]
        [InlineData("host_unreachable", NetworkErrorCodes.HostUnreachable)]
        [InlineData("network_unreachable", NetworkErrorCodes.NetworkUnreachable)]
        [InlineData("connection_closed", NetworkErrorCodes.ConnectionClosed)]
        [InlineData("dns_notfound", NetworkErrorCodes.DnsNotFound)]
        [InlineData("dns_no_data", NetworkErrorCodes.DnsNoData)]
        [InlineData("dns_try_again", NetworkErrorCodes.DnsTryAgain)]
        [InlineData("dns_failure", NetworkErrorCodes.DnsFailure)]
        [InlineData("tls_cert_expired", NetworkErrorCodes.TlsCertExpired)]
        [InlineData("tls_cert_hostname_mismatch", NetworkErrorCodes.TlsCertHostnameMismatch)]
        [InlineData("tls_cert_untrusted", NetworkErrorCodes.TlsCertUntrusted)]
        [InlineData("tls_cert_invalid", NetworkErrorCodes.TlsCertInvalid)]
        [InlineData("tls_handshake_failure", NetworkErrorCodes.TlsHandshakeFailure)]
        [InlineData("protocol_error", NetworkErrorCodes.ProtocolError)]
        [InlineData("rule_failure", NetworkErrorCodes.RuleFailure)]
        [InlineData("unknown", NetworkErrorCodes.Unknown)]
        public void Token_Strings_Are_Stable(string expected, string actual)
        {
            Assert.Equal(expected, actual);
        }
    }
}
