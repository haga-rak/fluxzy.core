// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Fluxzy.Certificates
{
    /// <summary>
    /// An utility to create fluxzy compatatible root CA
    /// </summary>
    public class CertificateBuilder
    {
        private readonly CertificateBuilderOptions _options;

        /// <summary>
        /// Create a new instance with option
        /// </summary>
        /// <param name="options"></param>
        public CertificateBuilder(CertificateBuilderOptions options)
        {
            options.Validate();
            _options = options; 
        }

        /// <summary>
        /// Returns a PKCS12 certificate as a byte array
        /// </summary>
        /// <returns></returns>
        public byte[] CreateSelfSigned()
        {
            using var privateKey = RSA.Create(_options.KeySize);
            
            var certificateRequest = new CertificateRequest(
                _options.Format(),
                privateKey,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            certificateRequest.CertificateExtensions.Add(
                new X509KeyUsageExtension(
                    X509KeyUsageFlags.KeyCertSign
                    | X509KeyUsageFlags.CrlSign
                    | X509KeyUsageFlags.DigitalSignature,
                    false));

            certificateRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 10, false));

            certificateRequest.CertificateExtensions.Add(
                new X509EnhancedKeyUsageExtension(
                    new OidCollection {
                        new("1.3.6.1.5.5.7.3.1"),
                        new("1.3.6.1.5.5.7.3.2")
                    },
                    false));

            byte[] data = new byte[20];

            new Random().NextBytes(data);

            var subjectExtension = new X509SubjectKeyIdentifierExtension(certificateRequest.PublicKey, false);

            certificateRequest.CertificateExtensions.Add(subjectExtension);
            certificateRequest.CertificateExtensions.Add(
                new X509AuthorityKeyIdentifierExtension2(subjectExtension.RawData, false));

            using var cert =
                certificateRequest.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddDays(
                    _options.DaysBeforeExpiration));

            if (_options.P12Password != null)
                return cert.Export(X509ContentType.Pkcs12, _options.P12Password);

            return cert.Export(X509ContentType.Pkcs12);
        }
    }

    internal class X509AuthorityKeyIdentifierExtension2 : X509Extension
    {
        private static Oid AuthorityKeyIdentifierOid => new Oid("2.5.29.35");

        public X509AuthorityKeyIdentifierExtension2(byte[] subjectIdentifierRawData, bool critical)
            : base(AuthorityKeyIdentifierOid, EncodeExtension(subjectIdentifierRawData), critical)
        {
        }

        private static byte[] EncodeExtension(byte[] subjectIdentifierRawData)
        {
            var rawData = subjectIdentifierRawData;
            var segment = new ArraySegment<byte>(rawData, 2, rawData.Length - 2);
            var authorityKeyIdentifier = new byte[segment.Count + 4];
            // KeyID of the AuthorityKeyIdentifier
            authorityKeyIdentifier[0] = 0x30;
            authorityKeyIdentifier[1] = 0x16;
            authorityKeyIdentifier[2] = 0x80;
            authorityKeyIdentifier[3] = 0x14;
            segment.CopyTo(authorityKeyIdentifier, 4);
            return authorityKeyIdentifier;
        }
    }
}
