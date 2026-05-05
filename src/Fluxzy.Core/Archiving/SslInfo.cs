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
            RemoteCertificateSerialNumber = remote?.SerialNumber;

            LocalCertificateNotBefore = local?.NotBefore;
            LocalCertificateNotAfter = local?.NotAfter;
            LocalCertificateSerialNumber = local?.SerialNumber;

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
            NegotiatedApplicationProtocol = clientProtocol.GetApplicationProtocol().ToString();
            SslProtocol = clientProtocol.GetSChannelProtocol();

            NegotiatedCipherSuite = (TlsCipherSuite) clientProtocol.SessionParameters.CipherSuite;
            (CipherAlgorithm, HashAlgorithm) = DeriveAlgorithmsFromCipherSuite(NegotiatedCipherSuite);

            if (BcCertificateHelper.TryReadDetailedInfo(clientProtocol.SessionParameters.LocalCertificate,
                    out var localSubject, out var localIssuer,
                    out var localNotBefore, out var localNotAfter, out var localSerial)) {
                LocalCertificateIssuer = localIssuer;
                LocalCertificateSubject = localSubject;
                LocalCertificateNotBefore = localNotBefore;
                LocalCertificateNotAfter = localNotAfter;
                LocalCertificateSerialNumber = localSerial;

                if (dumpCertificate) {
                    LocalCertificatePem = clientProtocol.SessionParameters.LocalCertificate
                                                        .ToPem();
                }

            }

            if (BcCertificateHelper.TryReadDetailedInfo(clientProtocol.SessionParameters.PeerCertificate,
                    out var remoteSubject, out var remoteIssuer,
                    out var remoteNotBefore, out var remoteNotAfter, out var remoteSerial)) {
                RemoteCertificateIssuer = remoteIssuer;
                RemoteCertificateSubject = remoteSubject;
                RemoteCertificateNotBefore = remoteNotBefore;
                RemoteCertificateNotAfter = remoteNotAfter;
                RemoteCertificateSerialNumber = remoteSerial;

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
            string? remoteCertificateSerialNumber,
            DateTime? localCertificateNotBefore, DateTime? localCertificateNotAfter,
            string? localCertificateSerialNumber)
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
            RemoteCertificateSerialNumber = remoteCertificateSerialNumber;
            LocalCertificateNotBefore = localCertificateNotBefore;
            LocalCertificateNotAfter = localCertificateNotAfter;
            LocalCertificateSerialNumber = localCertificateSerialNumber;
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

        public string? RemoteCertificateSerialNumber { get; }

        public DateTime? LocalCertificateNotBefore { get; }

        public DateTime? LocalCertificateNotAfter { get; }

        public string? LocalCertificateSerialNumber { get; }

        private static (CipherAlgorithmType Cipher, HashAlgorithmType Hash) DeriveAlgorithmsFromCipherSuite(
            TlsCipherSuite cipherSuite)
        {
            var name = cipherSuite.ToString();

            // TLS 1.2 and earlier use TLS_<kex>_WITH_<cipher>_<hash>; TLS 1.3 uses TLS_<cipher>_<hash>.
            var withIdx = name.IndexOf("_WITH_", StringComparison.Ordinal);
            var body = withIdx >= 0
                ? name.Substring(withIdx + "_WITH_".Length)
                : name.StartsWith("TLS_", StringComparison.Ordinal) ? name.Substring(4) : name;

            var lastUnderscore = body.LastIndexOf('_');
            var hashName = lastUnderscore >= 0 ? body.Substring(lastUnderscore + 1) : string.Empty;
            var cipherPart = lastUnderscore >= 0 ? body.Substring(0, lastUnderscore) : body;

            var hash = hashName switch {
                "SHA" => HashAlgorithmType.Sha1,
                "SHA256" => HashAlgorithmType.Sha256,
                "SHA384" => HashAlgorithmType.Sha384,
                "SHA512" => HashAlgorithmType.Sha512,
                "MD5" => HashAlgorithmType.Md5,
                _ => HashAlgorithmType.None
            };

            var cipher = CipherAlgorithmType.None;

            if (cipherPart.Contains("AES_128"))
                cipher = CipherAlgorithmType.Aes128;
            else if (cipherPart.Contains("AES_256"))
                cipher = CipherAlgorithmType.Aes256;
            else if (cipherPart.Contains("AES_192"))
                cipher = CipherAlgorithmType.Aes192;
            else if (cipherPart.Contains("3DES"))
                cipher = CipherAlgorithmType.TripleDes;
            else if (cipherPart.Contains("DES"))
                cipher = CipherAlgorithmType.Des;
            else if (cipherPart.Contains("RC4"))
                cipher = CipherAlgorithmType.Rc4;
            else if (cipherPart.Contains("RC2"))
                cipher = CipherAlgorithmType.Rc2;
            else if (cipherPart.Contains("NULL"))
                cipher = CipherAlgorithmType.Null;

            return (cipher, hash);
        }
    }
}
