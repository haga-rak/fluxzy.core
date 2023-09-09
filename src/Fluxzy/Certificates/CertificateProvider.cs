// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Concurrent;
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
        private readonly ICertificateCache _certCache;
        private readonly ConcurrentDictionary<string, Lazy<byte[]>> _certificateRepository = new();
        private readonly X509Certificate2 _rootCertificate;

        private readonly RSA _rsaKeyEngine = RSA.Create(2048);
        private readonly ConcurrentDictionary<string, X509Certificate2> _solveCertificateRepository = new();

        public CertificateProvider(
            FluxzySetting startupSetting,
            ICertificateCache certCache)
        {
            _certCache = certCache;

            _rootCertificate =
                startupSetting.CaCertificate.GetX509Certificate();

            // Warming : pre uild certicate 
            BuildCertificateForRootDomain("domain.com");
        }

        public X509Certificate2 GetCertificate(string hostName)
        {
            hostName = GetRootDomain(hostName);

            if (_solveCertificateRepository.TryGetValue(hostName, out var value))
                return value;

            lock (string.Intern(hostName)) {
                if (_solveCertificateRepository.TryGetValue(hostName, out value))
                    return value;

                var lazyCertificate =
                    _certificateRepository.GetOrAdd(hostName, new Lazy<byte[]>(() =>
                            _certCache.Load(_rootCertificate.SerialNumber!, hostName, BuildCertificateForRootDomain),
                        true));

                var val = lazyCertificate.Value;

                var r = new X509Certificate2(val);

                _solveCertificateRepository[hostName] = r;

                return r;
            }
        }

        public void Dispose()
        {
            _rsaKeyEngine.Dispose();
        }

        private string GetRootDomain(string hostName)
        {
            var splittedArray = hostName.Split('.');

            if (splittedArray.Length <= 2)
                return hostName;

            return string.Join(".", splittedArray.Reverse().Take(splittedArray.Length - 1).Reverse());
        }

        private byte[] BuildCertificateForRootDomain(string rootDomain)
        {
            Console.WriteLine("Building");

            var randomGenerator = new Random();

            var certificateRequest = new CertificateRequest(
                $"CN=*.{rootDomain}",
                _rsaKeyEngine,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            certificateRequest.CertificateExtensions.Add(
                new X509KeyUsageExtension(
                    X509KeyUsageFlags.DigitalSignature
                    | X509KeyUsageFlags.DataEncipherment
                    | X509KeyUsageFlags.KeyEncipherment
                    ,
                    true));

            var alternativeName = new SubjectAlternativeNameBuilder();

            alternativeName.AddDnsName(rootDomain);
            alternativeName.AddDnsName($"*.{rootDomain}");

            certificateRequest.CertificateExtensions.Add(alternativeName.Build());

            certificateRequest.CertificateExtensions.Add(
                new X509AuthorityKeyIdentifierExtension(_rootCertificate, false));

            certificateRequest.CertificateExtensions.Add(
                new X509EnhancedKeyUsageExtension(
                    new OidCollection {
                        new("1.3.6.1.5.5.7.3.1")
                    },
                    true));

            certificateRequest.CertificateExtensions.Add(
                new X509SubjectKeyIdentifierExtension(certificateRequest.PublicKey, false)
            );

            // Sign the certificate according to parent 

            var offSetEnd = new DateTimeOffset(_rootCertificate.NotAfter.AddSeconds(-1));
            var offsetLimit = new DateTimeOffset(DateTime.UtcNow.AddMonths(12));

            if (offSetEnd > offsetLimit)
                offSetEnd = offsetLimit;

            var buffer = new byte[16];
            randomGenerator.NextBytes(buffer);

            using var cert = certificateRequest.Create(_rootCertificate,
                new DateTimeOffset(_rootCertificate.NotBefore.AddSeconds(1)),
                offSetEnd,
                buffer);

            using var privateKeyCertificate = cert.CopyWithPrivateKey(_rsaKeyEngine);

            return privateKeyCertificate.Export(X509ContentType.Pkcs12);
        }
    }
}
