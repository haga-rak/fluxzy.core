// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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

        private readonly ICertificateCache _certCache;
        private readonly ConcurrentDictionary<string, Lazy<byte[]>> _certificateRepository = new();
        private readonly ECDsa _defaultEcdsaKeyEngine = ECDsa.Create()!;

        private readonly RSA _defaultRsaKeyEngine = RSA.Create(2048);

        private readonly AsymmetricAlgorithm _privateKey;
        private readonly X509Certificate2 _rootCertificate;

        private readonly ConcurrentDictionary<string, X509Certificate2> _solveCertificateRepository = new();

        public CertificateProvider(
            FluxzySetting startupSetting,
            ICertificateCache certCache)
        {
            _certCache = certCache;

            _rootCertificate =
                startupSetting.CaCertificate.GetX509Certificate();

            var pk = _rootCertificate.PublicKey;

            _privateKey = _rootCertificate.GetECDsaPublicKey() == null ? _defaultRsaKeyEngine : _defaultEcdsaKeyEngine;

            // Warming : pre uild certicate 
            BuildCertificateForRootDomain(_rootCertificate, _privateKey, "domain.com");
        }

        public X509Certificate2 GetCertificate(string hostName)
        {
            hostName = GetRootDomain(hostName);

            if (_solveCertificateRepository.TryGetValue(hostName, out var value)) {
                return value;
            }

            lock (string.Intern(hostName)) {
                if (_solveCertificateRepository.TryGetValue(hostName, out value)) {
                    return value;
                }

                var lazyCertificate =
                    _certificateRepository.GetOrAdd(hostName, new Lazy<byte[]>(() =>
                            _certCache.Load(_rootCertificate.SerialNumber!, hostName,
                                rootDomain => BuildCertificateForRootDomain(_rootCertificate, _privateKey, rootDomain)),
                        true));

                var val = lazyCertificate.Value;

                var r = new X509Certificate2(val);

                _solveCertificateRepository[hostName] = r;

                return r;
            }
        }

        public void Dispose()
        {
            _defaultRsaKeyEngine.Dispose();
            _defaultEcdsaKeyEngine.Dispose();

            foreach (var (_ , certificate) in _solveCertificateRepository) {
                certificate.Dispose();
            }
        }

        private string GetRootDomain(string hostName)
        {
            var splittedArray = hostName.Split('.');

            if (splittedArray.Length <= 2) {
                return hostName;
            }

            return string.Join(".", splittedArray.Reverse().Take(splittedArray.Length - 1).Reverse());
        }

        private static byte[] BuildCertificateForRootDomain(
            X509Certificate2 rootCertificate,
            AsymmetricAlgorithm privateKey, string rootDomain)
        {
            if (privateKey is RSA rsa) {
                return InternalBuildCertificateForRootDomain(rootCertificate, rsa, rootDomain);
            }

            if (privateKey is ECDsa ecdsa) {
                return InternalBuildCertificateForRootDomain(rootCertificate, ecdsa, rootDomain);
            }

            throw new NotSupportedException($"The private key type {privateKey.GetType()} is not supported");
        }


        private static byte[] InternalBuildCertificateForRootDomain(
            X509Certificate2 rootCertificate,
            RSA privateKey, string rootDomain)
        {
            var randomGenerator = new Random();

            var certificateRequest = new CertificateRequest(
                $"CN=*.{rootDomain}",
                privateKey,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            foreach (var extension in InvariantCaExtensions)
            {
                certificateRequest.CertificateExtensions.Add(extension);
            }

            var alternativeName = new SubjectAlternativeNameBuilder();
            alternativeName.AddDnsName(rootDomain);
            alternativeName.AddDnsName($"*.{rootDomain}");

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

            using var cert = certificateRequest.Create(rootCertificate,
                new DateTimeOffset(rootCertificate.NotBefore.AddSeconds(1)),
                offSetEnd,
                buffer);

            using var privateKeyCertificate = cert.CopyWithPrivateKey(privateKey);

            return privateKeyCertificate.Export(X509ContentType.Pkcs12);
        }

        private static byte[] InternalBuildCertificateForRootDomain(
            X509Certificate2 rootCertificate,
            ECDsa privateKey, string rootDomain)
        {
            var randomGenerator = new Random();

            var certificateRequest = new CertificateRequest(
                $"CN=*.{rootDomain}",
                privateKey,
                HashAlgorithmName.SHA256);

            foreach (var extension in InvariantCaExtensions)
            {
                certificateRequest.CertificateExtensions.Add(extension);
            }

            var alternativeName = new SubjectAlternativeNameBuilder();

            alternativeName.AddDnsName(rootDomain);
            alternativeName.AddDnsName($"*.{rootDomain}");

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

            using var cert = certificateRequest.Create(rootCertificate,
                new DateTimeOffset(rootCertificate.NotBefore.AddSeconds(1)),
                offSetEnd,
                buffer);

            using var privateKeyCertificate = cert.CopyWithPrivateKey(privateKey);

            return privateKeyCertificate.Export(X509ContentType.Pkcs12);
        }
    }
}
