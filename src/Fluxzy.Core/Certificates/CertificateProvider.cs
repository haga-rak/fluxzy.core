// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Fluxzy.Core;

namespace Fluxzy.Certificates
{
    /// <summary>
    ///     This implementation of ICertificateProvier is based on System.Security.Cryptography
    /// </summary>
    public class CertificateProvider : ICertificateProvider
    {
        private readonly ICertificateCache _certCache;
        private readonly ConcurrentDictionary<string, Lazy<byte[]>> _certificateRepository = new();

        // Exactly one of these engines is created, depending on the root CA key algorithm.
        // It holds the (shared) private key reused by every generated leaf certificate.
        private readonly ECDsa? _defaultEcdsaKeyEngine;
        private readonly RSA? _defaultRsaKeyEngine;

        private readonly AsymmetricAlgorithm _privateKey;
        private readonly X509Certificate2 _rootCertificate;

        private readonly ConcurrentDictionary<string, X509Certificate2> _solveCertificateRepository = new();

        private readonly Dictionary<string, string> _rootDomainCache = new(StringComparer.OrdinalIgnoreCase);

        public CertificateProvider(
            Certificate rootCertificate,
            ICertificateCache certCache)
        {
            _certCache = certCache;

            _rootCertificate =
                rootCertificate.GetX509Certificate();

            using (var rootEcdsaPublicKey = _rootCertificate.GetECDsaPublicKey()) {
                if (rootEcdsaPublicKey != null) {
                    // When the root CA is an ECDSA certificate, leaf certificates must also use an
                    // ECDSA key: CertificateRequest.Create() requires the issuer and the request to
                    // share the same public key OID. The leaf key is generated on a NIST curve that
                    // tracks the root CA strength, but never below P-256: P-224 has no TLS 1.3
                    // signature scheme (RFC 8446) and would be unusable for a server certificate.
                    _defaultEcdsaKeyEngine = ECDsa.Create(GetLeafCurve(rootEcdsaPublicKey.KeySize));
                    _privateKey = _defaultEcdsaKeyEngine;
                }
                else {
                    _defaultRsaKeyEngine = RSA.Create(2048);
                    _privateKey = _defaultRsaKeyEngine;
                }
            }

            // Warming : pre build certificate
            BuildCertificateForRootDomain(_rootCertificate, _privateKey, "domain.com");
        }

        private static List<X509Extension> InvariantCaExtensions { get; } = new() {
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature
                | X509KeyUsageFlags.DataEncipherment
                | X509KeyUsageFlags.KeyEncipherment
                ,
                true),

            new X509EnhancedKeyUsageExtension(
                new OidCollection {
                    new("1.3.6.1.5.5.7.3.1")
                },
                true)
        };

        public X509Certificate2 GetCertificate(string rootDomain)
        {
            var cnName = GetRootDomain(rootDomain);

            if (_solveCertificateRepository.TryGetValue(cnName, out var value)) {
                if (!IsCertificateExpired(value))
                    return value;

                // Certificate is expired, need to regenerate under lock
            }

            lock (string.Intern(cnName)) {
                if (_solveCertificateRepository.TryGetValue(cnName, out value)) {
                    if (!IsCertificateExpired(value))
                        return value;

                    // Remove expired certificate from in-memory caches
                    _solveCertificateRepository.TryRemove(cnName, out var expiredCert);
                    _certificateRepository.TryRemove(cnName, out _);
                    expiredCert?.Dispose();
                }

                var lazyCertificate =
                    _certificateRepository.GetOrAdd(cnName, new Lazy<byte[]>(() =>
                            _certCache.Load(_rootCertificate.SerialNumber!, cnName,
                                rD => BuildCertificateForRootDomain(_rootCertificate, _privateKey, rD)),
                        true));

                var val = lazyCertificate.Value;

                var r = new X509Certificate2(val);

                _solveCertificateRepository[cnName] = r;

                return r;
            }
        }

        /// <summary>
        ///  Pick the NIST curve used for leaf ECDSA keys based on the root CA key strength.
        ///  Clamped to a minimum of P-256 so the generated server certificates remain usable
        ///  for TLS 1.3 (which defines ECDSA signature schemes for P-256/P-384/P-521 only).
        /// </summary>
        private static ECCurve GetLeafCurve(int rootKeySize)
        {
            return rootKeySize switch {
                >= 521 => ECCurve.NamedCurves.nistP521,
                >= 384 => ECCurve.NamedCurves.nistP384,
                _ => ECCurve.NamedCurves.nistP256
            };
        }

        private static bool IsCertificateExpired(X509Certificate2 certificate)
        {
            // Use a 1-minute buffer before actual expiration to avoid edge cases
            return certificate.NotAfter <= DateTime.UtcNow.AddMinutes(1);
        }

        public void Dispose()
        {
            _defaultRsaKeyEngine?.Dispose();
            _defaultEcdsaKeyEngine?.Dispose();

            foreach (var (_, certificate) in _solveCertificateRepository) {
                certificate.Dispose();
            }
        }

        internal byte[] GetCertificateBytes(string hostName)
        {
            hostName = GetRootDomain(hostName);

            return BuildCertificateForRootDomain(_rootCertificate, _privateKey, hostName);
        }

        protected virtual string GetRootDomain(string hostName)
        {
            if (FluxzySharedSetting.NoCacheOnFqdn) {
                return PublicSuffixHelper.GetRootDomain(hostName);
            }

            lock (_rootDomainCache)
            {
                if (_rootDomainCache.TryGetValue(hostName, out var value))
                {
                    return value;
                }

                var result = PublicSuffixHelper.GetRootDomain(hostName);

                _rootDomainCache[hostName] = result;

                return result;
            }
        }

        private static byte[] BuildCertificateForRootDomain(
            X509Certificate2 rootCertificate,
            AsymmetricAlgorithm privateKey, string cnName)
        {
            if (privateKey is RSA rsa) {
                return InternalBuildCertificateForRootDomain(rootCertificate, rsa, cnName);
            }

            if (privateKey is ECDsa ecdsa) {
                return InternalBuildCertificateForRootDomain(rootCertificate, ecdsa, cnName);
            }

            throw new NotSupportedException($"The private key type {privateKey.GetType()} is not supported");
        }

        private static byte[] InternalBuildCertificateForRootDomain(
            X509Certificate2 rootCertificate,
            RSA privateKey, string cnName)
        {
            var randomGenerator = new Random();
            var isIpAddress = IPAddress.TryParse(cnName, out var ipAddress);

            var certificateRequest = new CertificateRequest(
                isIpAddress ? $"CN={cnName}" : $"CN=*.{cnName}",
                privateKey,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            foreach (var extension in InvariantCaExtensions) {
                certificateRequest.CertificateExtensions.Add(extension);
            }

            var alternativeName = new SubjectAlternativeNameBuilder();

            if (isIpAddress) {
                alternativeName.AddIpAddress(ipAddress!);
            }
            else {
                alternativeName.AddDnsName(cnName);
                alternativeName.AddDnsName($"*.{cnName}");
            }

            certificateRequest.CertificateExtensions.Add(alternativeName.Build());

            certificateRequest.CertificateExtensions.Add(
                new X509AuthorityKeyIdentifierExtension(rootCertificate, false));

            certificateRequest.CertificateExtensions.Add(
                new X509SubjectKeyIdentifierExtension(certificateRequest.PublicKey, false)
            );

            // Sign the certificate according to parent 

            var offSetEnd = new DateTimeOffset(rootCertificate.NotAfter.AddSeconds(-1));
            var offsetLimit = new DateTimeOffset(DateTime.UtcNow.AddMonths(12));

            if (offSetEnd > offsetLimit) {
                offSetEnd = offsetLimit;
            }

#if NET5_0_OR_GREATER

            Span<byte> buffer = stackalloc byte[16];
#else
            var buffer = new byte[16];
#endif

            randomGenerator.NextBytes(buffer); // TODO check for collision here 

            // Start the leaf no earlier than the root CA, but never in the future: a freshly
            // created root has a NotBefore set to "now", so adding any positive offset would
            // make the leaf not-yet-valid until the wall clock catches up.
            var notBefore = rootCertificate.NotBefore;
            var now = DateTime.Today;
            notBefore = now < notBefore ? notBefore : now;

            using var cert = certificateRequest.Create(rootCertificate,
                new DateTimeOffset(notBefore),
                offSetEnd,
                buffer);

            using var privateKeyCertificate = cert.CopyWithPrivateKey(privateKey);

            return privateKeyCertificate.Export(X509ContentType.Pkcs12);
        }

        private static byte[] InternalBuildCertificateForRootDomain(
            X509Certificate2 rootCertificate,
            ECDsa privateKey, string cnName)
        {
            var randomGenerator = new Random();
            var isIpAddress = IPAddress.TryParse(cnName, out var ipAddress);

            var certificateRequest = new CertificateRequest(
                isIpAddress ? $"CN={cnName}" : $"CN=*.{cnName}",
                privateKey,
                HashAlgorithmName.SHA256);

            foreach (var extension in InvariantCaExtensions) {
                certificateRequest.CertificateExtensions.Add(extension);
            }

            var alternativeName = new SubjectAlternativeNameBuilder();

            if (isIpAddress) {
                alternativeName.AddIpAddress(ipAddress!);
            }
            else {
                alternativeName.AddDnsName(cnName);
                alternativeName.AddDnsName($"*.{cnName}");
            }

            certificateRequest.CertificateExtensions.Add(alternativeName.Build());

            certificateRequest.CertificateExtensions.Add(
                new X509AuthorityKeyIdentifierExtension(rootCertificate, false));

            certificateRequest.CertificateExtensions.Add(
                new X509SubjectKeyIdentifierExtension(certificateRequest.PublicKey, false)
            );

            // Sign the certificate according to parent 

            var offSetEnd = new DateTimeOffset(rootCertificate.NotAfter.AddSeconds(-1));
            var offsetLimit = new DateTimeOffset(DateTime.UtcNow.AddMonths(12));

            if (offSetEnd > offsetLimit) {
                offSetEnd = offsetLimit;
            }

#if NET5_0_OR_GREATER

            Span<byte> buffer = stackalloc byte[16];
#else
            var buffer = new byte[16];
#endif

            randomGenerator.NextBytes(buffer); // TODO check for collision here 

            // Start the leaf no earlier than the root CA, but never in the future: a freshly
            // created root has a NotBefore set to "now", so adding any positive offset would
            // make the leaf not-yet-valid until the wall clock catches up.
            var notBefore = rootCertificate.NotBefore;
            var now = DateTime.Today;
            notBefore = now < notBefore ? notBefore : now;

            using var cert = certificateRequest.Create(rootCertificate,
                new DateTimeOffset(notBefore),
                offSetEnd,
                buffer);

            using var privateKeyCertificate = cert.CopyWithPrivateKey(privateKey);

            return privateKeyCertificate.Export(X509ContentType.Pkcs12);
        }
    }
}
