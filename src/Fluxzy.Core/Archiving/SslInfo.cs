// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net.Security;
using System.Security.Authentication;
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
            RemoteCertificateSubject = sslStream.RemoteCertificate?.Subject;
            RemoteCertificateIssuer = sslStream.RemoteCertificate?.Issuer;
            LocalCertificateIssuer = sslStream.LocalCertificate?.Issuer;
            LocalCertificateSubject = sslStream.LocalCertificate?.Subject;
            SslProtocol = sslStream.SslProtocol;

            if (dumpCertificate)
            {
                RemoteCertificatePem = sslStream.RemoteCertificate?.ToPem();
                LocalCertificatePem = sslStream.RemoteCertificate?.ToPem();
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

            if (BcCertificateHelper.ReadInfo(clientProtocol.SessionParameters.LocalCertificate,
                    out var localSubject, out var localIssuer)) {
                LocalCertificateIssuer = localIssuer;
                LocalCertificateSubject = localSubject;

                if (dumpCertificate) {
                    LocalCertificatePem = clientProtocol.SessionParameters.LocalCertificate
                                                        .ToPem();
                }

            }

            if (BcCertificateHelper.ReadInfo(clientProtocol.SessionParameters.PeerCertificate,
                    out var remoteSubject, out var remoteIssuer)) {
                RemoteCertificateIssuer = remoteIssuer;
                RemoteCertificateSubject = remoteSubject;

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
            string ? localCertificatePem, string ? remoteCertificatePem)
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
    }
}
