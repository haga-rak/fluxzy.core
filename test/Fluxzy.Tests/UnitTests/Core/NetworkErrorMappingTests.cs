// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Fluxzy.Clients.Ssl.BouncyCastle;
using Fluxzy.Clients.Ssl.SChannel;
using Fluxzy.Core;
using Fluxzy.Rules;
using Org.BouncyCastle.Tls;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Core
{
    public class NetworkErrorMappingTests
    {
        [Theory]
        [InlineData((short) AlertDescription.certificate_expired, NetworkErrorCodes.TlsCertExpired)]
        [InlineData((short) AlertDescription.bad_certificate, NetworkErrorCodes.TlsCertHostnameMismatch)]
        [InlineData((short) AlertDescription.unknown_ca, NetworkErrorCodes.TlsCertUntrusted)]
        [InlineData((short) AlertDescription.certificate_unknown, NetworkErrorCodes.TlsCertInvalid)]
        [InlineData((short) AlertDescription.certificate_required, NetworkErrorCodes.TlsCertInvalid)]
        [InlineData((short) AlertDescription.certificate_revoked, NetworkErrorCodes.TlsCertInvalid)]
        [InlineData((short) AlertDescription.handshake_failure, NetworkErrorCodes.TlsHandshakeFailure)]
        [InlineData((short) AlertDescription.internal_error, NetworkErrorCodes.TlsHandshakeFailure)]
        public void BouncyCastle_MapTlsAlert_Returns_Expected_Token(short alertDescription, string expected)
        {
            var alert = new TlsFatalAlert(alertDescription);
            Assert.Equal(expected, BouncyCastleConnectionBuilder.MapTlsAlert(alert));
        }

        [Fact]
        public void BouncyCastle_MapTlsAlert_Walks_InnerException_Chain()
        {
            var inner = new TlsFatalAlert(AlertDescription.certificate_expired);
            var wrapper = new InvalidOperationException("wrapper", inner);

            Assert.Equal(NetworkErrorCodes.TlsCertExpired, BouncyCastleConnectionBuilder.MapTlsAlert(wrapper));
        }

        [Fact]
        public void BouncyCastle_MapTlsAlert_Falls_Back_When_No_Alert_Found()
        {
            var ex = new InvalidOperationException("no alert in chain");
            Assert.Equal(NetworkErrorCodes.TlsHandshakeFailure, BouncyCastleConnectionBuilder.MapTlsAlert(ex));
        }

        [Theory]
        [InlineData(SslPolicyErrors.RemoteCertificateNameMismatch, X509ChainStatusFlags.NoError, NetworkErrorCodes.TlsCertHostnameMismatch)]
        [InlineData(SslPolicyErrors.RemoteCertificateChainErrors, X509ChainStatusFlags.NotTimeValid, NetworkErrorCodes.TlsCertExpired)]
        [InlineData(SslPolicyErrors.RemoteCertificateChainErrors, X509ChainStatusFlags.UntrustedRoot, NetworkErrorCodes.TlsCertUntrusted)]
        [InlineData(SslPolicyErrors.RemoteCertificateChainErrors, X509ChainStatusFlags.PartialChain, NetworkErrorCodes.TlsCertUntrusted)]
        [InlineData(SslPolicyErrors.RemoteCertificateChainErrors, X509ChainStatusFlags.NoIssuanceChainPolicy, NetworkErrorCodes.TlsCertUntrusted)]
        [InlineData(SslPolicyErrors.RemoteCertificateChainErrors, X509ChainStatusFlags.Revoked, NetworkErrorCodes.TlsCertInvalid)]
        [InlineData(SslPolicyErrors.RemoteCertificateNotAvailable, X509ChainStatusFlags.NoError, NetworkErrorCodes.TlsCertInvalid)]
        [InlineData(SslPolicyErrors.None, X509ChainStatusFlags.NoError, NetworkErrorCodes.TlsHandshakeFailure)]
        public void SChannel_MapPolicy_Returns_Expected_Token(
            SslPolicyErrors policy, X509ChainStatusFlags chain, string expected)
        {
            Assert.Equal(expected, SChannelConnectionBuilder.MapPolicyToNetworkErrorCode(policy, chain));
        }

        [Fact]
        public void Resolve_Returns_Unknown_For_Plain_Exception()
        {
            var ex = new InvalidOperationException("nothing special");
            Assert.Equal(NetworkErrorCodes.Unknown, ConnectionErrorHandler.ResolveNetworkErrorCode(ex));
        }

        [Fact]
        public void Resolve_Returns_TlsHandshakeFailure_For_AuthenticationException()
        {
            var ex = new AuthenticationException("tls failed");
            Assert.Equal(NetworkErrorCodes.TlsHandshakeFailure, ConnectionErrorHandler.ResolveNetworkErrorCode(ex));
        }

        [Fact]
        public void Resolve_Returns_RuleFailure_For_RuleExecutionFailureException()
        {
            var ex = new RuleExecutionFailureException("rule kaboom", new InvalidOperationException("inner"));
            Assert.Equal(NetworkErrorCodes.RuleFailure, ConnectionErrorHandler.ResolveNetworkErrorCode(ex));
        }

        [Theory]
        [InlineData(NetworkErrorCodes.ProtocolError)]
        [InlineData(NetworkErrorCodes.ConnectionClosed)]
        [InlineData(NetworkErrorCodes.DnsFailure)]
        public void Resolve_Honours_PreTagged_ClientErrorException(string token)
        {
            var ex = new ClientErrorException(0, "msg", networkErrorCode: token);
            Assert.Equal(token, ConnectionErrorHandler.ResolveNetworkErrorCode(ex));
        }

        [Fact]
        public void Resolve_Walks_InnerException_Chain()
        {
            var inner = new ClientErrorException(0, "inner", networkErrorCode: NetworkErrorCodes.ProtocolError);
            var outer = new InvalidOperationException("outer", inner);

            Assert.Equal(NetworkErrorCodes.ProtocolError, ConnectionErrorHandler.ResolveNetworkErrorCode(outer));
        }
    }
}
