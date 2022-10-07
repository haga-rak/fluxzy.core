using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Fluxzy.Core;

namespace Fluxzy
{
    public class CertificateProvider : ICertificateProvider
    {
        private readonly X509Certificate2 _baseCertificate;
        private readonly ICertificateCache _certCache;
        private readonly ConcurrentDictionary<string, Lazy<byte[]>> _certificateRepository = new();

        private readonly RSA _rsaKeyEngine = RSA.Create(2048);
        private readonly ConcurrentDictionary<string, X509Certificate2> _solveCertificateRepository = new();

        public CertificateProvider(
            FluxzySetting startupSetting,
            ICertificateCache certCache)
        {
            _certCache = certCache;

            _baseCertificate =
                startupSetting.CaCertificate.GetCertificate();

            // Warming : Make RSA Threadsafe
            BuildCertificateForRootDomain("domain.com");
        }

        public X509Certificate2
            GetCertificate(string hostName)
        {
            hostName = GetRootDomain(hostName);

            lock (string.Intern(hostName))
            {
                if (_solveCertificateRepository.TryGetValue(hostName, out var value))
                    return value;

                var lazyCertificate =
                    _certificateRepository.GetOrAdd(hostName, new Lazy<byte[]>(() =>
                        _certCache.Load(_baseCertificate.SerialNumber, hostName, BuildCertificateForRootDomain), true));

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
            var watch = new Stopwatch();
            var randomGenerator = new Random();

            watch.Start();

            var certificateRequest = new CertificateRequest(
                $"CN=*.{rootDomain}, OU={rootDomain.ToUpperInvariant()}",
                _rsaKeyEngine,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
            
            watch.Stop();

            certificateRequest.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(false, false, 0, false));

            certificateRequest.CertificateExtensions.Add(
                new X509KeyUsageExtension(
                    X509KeyUsageFlags.DigitalSignature
                    | X509KeyUsageFlags.DataEncipherment
                    | X509KeyUsageFlags.KeyEncipherment
                    | X509KeyUsageFlags.KeyCertSign
                    | X509KeyUsageFlags.NonRepudiation
                    | X509KeyUsageFlags.KeyAgreement
                    ,
                    false));

            var alternativeName = new SubjectAlternativeNameBuilder();

            alternativeName.AddDnsName(rootDomain);
            alternativeName.AddDnsName($"*.{rootDomain}");

            certificateRequest.CertificateExtensions.Add(alternativeName.Build());
            certificateRequest.CertificateExtensions.Add(
                new X509EnhancedKeyUsageExtension(
                    new OidCollection
                    {
                        new("1.3.6.1.5.5.7.3.1")
                    },
                    true));

            certificateRequest.CertificateExtensions.Add(
                new X509SubjectKeyIdentifierExtension(certificateRequest.PublicKey, false)
            );

            // Sign the certificate according to parent 

            var offSetEnd = new DateTimeOffset(_baseCertificate.NotAfter.AddSeconds(-1));
            var offsetLimit = new DateTimeOffset(DateTime.UtcNow.AddMonths(30));

            if (offSetEnd > offsetLimit) offSetEnd = offsetLimit;

            var buffer = new byte[16]; 
            randomGenerator.NextBytes(buffer);

            using var cert = certificateRequest.Create(_baseCertificate,
                new DateTimeOffset(_baseCertificate.NotBefore.AddSeconds(1)),
                offSetEnd,
                buffer);

            using var privateKeyCertificate = cert.CopyWithPrivateKey(_rsaKeyEngine);

            return privateKeyCertificate.Export(X509ContentType.Pkcs12);
        }
    }
}