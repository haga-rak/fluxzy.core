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
        /// <summary>
        ///  NIST P-224 (secp224r1) object identifier. Unlike P-256/P-384/P-521 this curve is not
        ///  exposed by <see cref="ECCurve.NamedCurves"/>, so it has to be built from its OID.
        /// </summary>
        private const string NistP224Oid = "1.3.132.0.33";

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
            if (_options.KeyAlgorithm == CertificateKeyAlgorithm.Rsa) {
                using var rsaKey = RSA.Create(_options.KeySize);

                var rsaRequest = new CertificateRequest(
                    _options.Format(),
                    rsaKey,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                return BuildSelfSigned(rsaRequest);
            }

            using var ecdsaKey = ECDsa.Create(GetEcCurve(_options.KeyAlgorithm));

            var ecdsaRequest = new CertificateRequest(
                _options.Format(),
                ecdsaKey,
                GetEcHashAlgorithm(_options.KeyAlgorithm));

            return BuildSelfSigned(ecdsaRequest);
        }

        /// <summary>
        ///  Append the invariant root CA extensions to the request, self-sign it and export it as PKCS12.
        ///  The request carries the key (RSA or ECDSA), so this is algorithm agnostic.
        /// </summary>
        private byte[] BuildSelfSigned(CertificateRequest certificateRequest)
        {
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

        /// <summary>
        ///  Resolve the elliptic curve associated with an ECDSA <see cref="CertificateKeyAlgorithm"/>.
        /// </summary>
        internal static ECCurve GetEcCurve(CertificateKeyAlgorithm keyAlgorithm)
        {
            return keyAlgorithm switch {
                CertificateKeyAlgorithm.EcdsaP224 => ECCurve.CreateFromValue(NistP224Oid),
                CertificateKeyAlgorithm.EcdsaP256 => ECCurve.NamedCurves.nistP256,
                CertificateKeyAlgorithm.EcdsaP384 => ECCurve.NamedCurves.nistP384,
                CertificateKeyAlgorithm.EcdsaP521 => ECCurve.NamedCurves.nistP521,
                _ => throw new ArgumentException($"{keyAlgorithm} is not an ECDSA algorithm", nameof(keyAlgorithm))
            };
        }

        /// <summary>
        ///  Pick a hash strength matching the curve order, as recommended for ECDSA signatures.
        /// </summary>
        private static HashAlgorithmName GetEcHashAlgorithm(CertificateKeyAlgorithm keyAlgorithm)
        {
            return keyAlgorithm switch {
                CertificateKeyAlgorithm.EcdsaP224 => HashAlgorithmName.SHA256,
                CertificateKeyAlgorithm.EcdsaP256 => HashAlgorithmName.SHA256,
                CertificateKeyAlgorithm.EcdsaP384 => HashAlgorithmName.SHA384,
                CertificateKeyAlgorithm.EcdsaP521 => HashAlgorithmName.SHA512,
                _ => throw new ArgumentException($"{keyAlgorithm} is not an ECDSA algorithm", nameof(keyAlgorithm))
            };
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
