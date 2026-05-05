// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;
using Fluxzy.Clients.Ssl.BouncyCastle;
using Fluxzy.Extensions;
#pragma warning disable SYSLIB0058

namespace Fluxzy
{
    /// <summary>
    ///    Represent a SSL information
    /// </summary>
    public class SslInfo
    {
        /// <summary>
        ///     Building  from Schannel
        /// </summary>
        /// <param name="sslStream"></param>
        /// <param name="dumpCertificate">export full certificate in ssl info</param>
        public SslInfo(SslStream sslStream, bool dumpCertificate)
        {
            NegotiatedCipherSuite = sslStream.NegotiatedCipherSuite;
            CipherAlgorithm = sslStream.CipherAlgorithm;
            HashAlgorithm = sslStream.HashAlgorithm;
            KeyExchangeAlgorithm = sslStream.KeyExchangeAlgorithm.ToString();
            NegotiatedApplicationProtocol = sslStream.NegotiatedApplicationProtocol.ToString();

            var remote = sslStream.RemoteCertificate as X509Certificate2;
            var local = sslStream.LocalCertificate as X509Certificate2;

            RemoteCertificateSubject = remote?.Subject;
            RemoteCertificateIssuer = remote?.Issuer;
            LocalCertificateIssuer = local?.Issuer;
            LocalCertificateSubject = local?.Subject;
            SslProtocol = sslStream.SslProtocol;

            RemoteCertificateNotBefore = remote?.NotBefore;
            RemoteCertificateNotAfter = remote?.NotAfter;
            RemoteCertificateSha1Thumbprint = remote?.GetCertHashString();

            LocalCertificateNotBefore = local?.NotBefore;
            LocalCertificateNotAfter = local?.NotAfter;
            LocalCertificateSha1Thumbprint = local?.GetCertHashString();

            if (dumpCertificate)
            {
                RemoteCertificatePem = remote?.ToPem();
                LocalCertificatePem = local?.ToPem();
            }
        }

        /// <summary>
        ///     Building from BouncyCastle
        /// </summary>
        /// <param name="clientProtocol"></param>
        /// <param name="dumpCertificate"></param>
        internal SslInfo(FluxzyClientProtocol clientProtocol, bool dumpCertificate)
        {
//#if NET6_0
//            CipherAlgorithm = ((System.Net.Security.TlsCipherSuite) clientProtocol.SessionParameters.CipherSuite).ToString();
//#endif

            NegotiatedApplicationProtocol = clientProtocol.GetApplicationProtocol().ToString();
            SslProtocol = clientProtocol.GetSChannelProtocol();

            if (BcCertificateHelper.TryReadDetailedInfo(clientProtocol.SessionParameters.LocalCertificate,
                    out var localSubject, out var localIssuer,
                    out var localNotBefore, out var localNotAfter, out var localSha1)) {
                LocalCertificateIssuer = localIssuer;
                LocalCertificateSubject = localSubject;
                LocalCertificateNotBefore = localNotBefore;
                LocalCertificateNotAfter = localNotAfter;
                LocalCertificateSha1Thumbprint = localSha1;

                if (dumpCertificate) {
                    LocalCertificatePem = clientProtocol.SessionParameters.LocalCertificate
                                                        .ToPem();
                }

            }

            if (BcCertificateHelper.TryReadDetailedInfo(clientProtocol.SessionParameters.PeerCertificate,
                    out var remoteSubject, out var remoteIssuer,
                    out var remoteNotBefore, out var remoteNotAfter, out var remoteSha1)) {
                RemoteCertificateIssuer = remoteIssuer;
                RemoteCertificateSubject = remoteSubject;
                RemoteCertificateNotBefore = remoteNotBefore;
                RemoteCertificateNotAfter = remoteNotAfter;
                RemoteCertificateSha1Thumbprint = remoteSha1;

                if (dumpCertificate) {
                    RemoteCertificatePem = clientProtocol.SessionParameters.PeerCertificate
                                                        .ToPem();
                }
            }

            KeyExchangeAlgorithm = string.Empty;
        }

        [JsonConstructor]
        public SslInfo(
            SslProtocols sslProtocol, string? remoteCertificateIssuer, string? remoteCertificateSubject,
            string? localCertificateSubject, string? localCertificateIssuer, string negotiatedApplicationProtocol,
            string keyExchangeAlgorithm, HashAlgorithmType hashAlgorithm,
            CipherAlgorithmType cipherAlgorithm, TlsCipherSuite negotiatedCipherSuite,
            string? localCertificatePem, string? remoteCertificatePem,
            DateTime? remoteCertificateNotBefore, DateTime? remoteCertificateNotAfter,
            string? remoteCertificateSha1Thumbprint,
            DateTime? localCertificateNotBefore, DateTime? localCertificateNotAfter,
            string? localCertificateSha1Thumbprint)
        {
            SslProtocol = sslProtocol;
            RemoteCertificateIssuer = remoteCertificateIssuer;
            RemoteCertificateSubject = remoteCertificateSubject;
            LocalCertificateSubject = localCertificateSubject;
            LocalCertificateIssuer = localCertificateIssuer;
            NegotiatedApplicationProtocol = negotiatedApplicationProtocol;
            KeyExchangeAlgorithm = keyExchangeAlgorithm;
            HashAlgorithm = hashAlgorithm;
            CipherAlgorithm = cipherAlgorithm;
            NegotiatedCipherSuite = negotiatedCipherSuite;
            LocalCertificatePem = localCertificatePem;
            RemoteCertificatePem = remoteCertificatePem;
            RemoteCertificateNotBefore = remoteCertificateNotBefore;
            RemoteCertificateNotAfter = remoteCertificateNotAfter;
            RemoteCertificateSha1Thumbprint = remoteCertificateSha1Thumbprint;
            LocalCertificateNotBefore = localCertificateNotBefore;
            LocalCertificateNotAfter = localCertificateNotAfter;
            LocalCertificateSha1Thumbprint = localCertificateSha1Thumbprint;
        }

        public SslProtocols SslProtocol { get; }

        public string? RemoteCertificateIssuer { get; }

        public string? RemoteCertificateSubject { get; }

        public string? LocalCertificateSubject { get; }

        public string? LocalCertificateIssuer { get; }

        public string NegotiatedApplicationProtocol { get; }

        public string KeyExchangeAlgorithm { get; }

        public HashAlgorithmType HashAlgorithm { get; }

        public CipherAlgorithmType CipherAlgorithm { get; }

        public byte[]? RemoteCertificate { get; set; }

        public byte[]? LocalCertificate { get; set; }

        public TlsCipherSuite NegotiatedCipherSuite { get;  }

        public string? LocalCertificatePem { get;  }

        public string? RemoteCertificatePem { get;  }

        public DateTime? RemoteCertificateNotBefore { get; }

        public DateTime? RemoteCertificateNotAfter { get; }

        public string? RemoteCertificateSha1Thumbprint { get; }

        public DateTime? LocalCertificateNotBefore { get; }

        public DateTime? LocalCertificateNotAfter { get; }

        public string? LocalCertificateSha1Thumbprint { get; }
    }
}
