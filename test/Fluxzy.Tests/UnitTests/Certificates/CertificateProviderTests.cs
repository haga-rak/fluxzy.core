// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Fluxzy.Certificates;
using Fluxzy.Core;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Certificates
{
    public class CertificateProviderTests
    {
        [Theory]
        [InlineData("192.168.1.1")]
        [InlineData("10.0.0.1")]
        [InlineData("127.0.0.1")]
        [InlineData("::1")]
        [InlineData("2001:db8::1")]
        public void GetCertificate_ForIpAddress_HasCorrectCN(string ipAddress)
        {
            // Arrange
            var rootCertificate = Certificate.UseDefault();
            using var provider = new CertificateProvider(rootCertificate, new InMemoryCertificateCache());

            // Act
            var cert = provider.GetCertificate(ipAddress);

            // Assert
            Assert.NotNull(cert);
            Assert.Equal($"CN={ipAddress}", cert.Subject);
        }

        [Theory]
        [InlineData("192.168.1.1")]
        [InlineData("10.0.0.1")]
        [InlineData("127.0.0.1")]
        public void GetCertificate_ForIpAddress_HasIpAddressInSAN(string ipAddress)
        {
            // Arrange
            var rootCertificate = Certificate.UseDefault();
            using var provider = new CertificateProvider(rootCertificate, new InMemoryCertificateCache());

            // Act
            var cert = provider.GetCertificate(ipAddress);

            // Assert
            var sanExtension = cert.Extensions
                .OfType<X509SubjectAlternativeNameExtension>()
                .FirstOrDefault();

            Assert.NotNull(sanExtension);

            var ipAddresses = sanExtension.EnumerateIPAddresses().ToList();
            Assert.Single(ipAddresses);
            Assert.Equal(IPAddress.Parse(ipAddress), ipAddresses[0]);
        }

        [Theory]
        [InlineData("192.168.1.1")]
        [InlineData("10.0.0.1")]
        public void GetCertificate_ForIpAddress_HasNoDnsNamesInSAN(string ipAddress)
        {
            // Arrange
            var rootCertificate = Certificate.UseDefault();
            using var provider = new CertificateProvider(rootCertificate, new InMemoryCertificateCache());

            // Act
            var cert = provider.GetCertificate(ipAddress);

            // Assert
            var sanExtension = cert.Extensions
                .OfType<X509SubjectAlternativeNameExtension>()
                .FirstOrDefault();

            Assert.NotNull(sanExtension);

            var dnsNames = sanExtension.EnumerateDnsNames().ToList();
            Assert.Empty(dnsNames);
        }

        [Theory]
        [InlineData("example.com")]
        [InlineData("www.example.com")]
        [InlineData("fluxzy.io")]
        public void GetCertificate_ForDomain_HasWildcardCN(string domain)
        {
            // Arrange
            var rootCertificate = Certificate.UseDefault();
            using var provider = new CertificateProvider(rootCertificate, new InMemoryCertificateCache());

            // Act
            var cert = provider.GetCertificate(domain);

            // Assert
            Assert.NotNull(cert);
            Assert.StartsWith("CN=*.", cert.Subject);
        }

        [Theory]
        [InlineData("example.com")]
        [InlineData("fluxzy.io")]
        public void GetCertificate_ForDomain_HasDnsNamesInSAN(string domain)
        {
            // Arrange
            var rootCertificate = Certificate.UseDefault();
            using var provider = new CertificateProvider(rootCertificate, new InMemoryCertificateCache());

            // Act
            var cert = provider.GetCertificate(domain);

            // Assert
            var sanExtension = cert.Extensions
                .OfType<X509SubjectAlternativeNameExtension>()
                .FirstOrDefault();

            Assert.NotNull(sanExtension);

            var dnsNames = sanExtension.EnumerateDnsNames().ToList();
            Assert.Contains(domain, dnsNames);
            Assert.Contains($"*.{domain}", dnsNames);
        }

        [Theory]
        [InlineData("example.com")]
        [InlineData("fluxzy.io")]
        public void GetCertificate_ForDomain_HasNoIpAddressesInSAN(string domain)
        {
            // Arrange
            var rootCertificate = Certificate.UseDefault();
            using var provider = new CertificateProvider(rootCertificate, new InMemoryCertificateCache());

            // Act
            var cert = provider.GetCertificate(domain);

            // Assert
            var sanExtension = cert.Extensions
                .OfType<X509SubjectAlternativeNameExtension>()
                .FirstOrDefault();

            Assert.NotNull(sanExtension);

            var ipAddresses = sanExtension.EnumerateIPAddresses().ToList();
            Assert.Empty(ipAddresses);
        }
    }
}
