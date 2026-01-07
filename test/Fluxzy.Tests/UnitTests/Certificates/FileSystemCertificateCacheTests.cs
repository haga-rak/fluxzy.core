// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using Fluxzy.Core;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Certificates
{
    public class FileSystemCertificateCacheTests : IDisposable
    {
        private const int ExpirationHeaderSize = sizeof(long);
        private readonly string _tempDirectory;
        private readonly FluxzySetting _setting;

        public FileSystemCertificateCacheTests()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), $"fluxzy_cache_test_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDirectory);

            _setting = FluxzySetting.CreateDefault();
            _setting.CertificateCacheDirectory = _tempDirectory;
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDirectory))
                Directory.Delete(_tempDirectory, true);
        }

        [Fact]
        public void Load_CacheMiss_CallsBuilderAndReturnsCertificate()
        {
            // Arrange
            var cache = new FileSystemCertificateCache(_setting);
            var expectedBytes = CreateFakeCertificateBytes(DateTime.UtcNow.AddMonths(6));
            var builderCallCount = 0;

            byte[] Builder(string domain)
            {
                builderCallCount++;
                return expectedBytes;
            }

            // Act
            var result = cache.Load("serial123", "example.com", Builder);

            // Assert
            Assert.Equal(1, builderCallCount);
            Assert.Equal(expectedBytes, result);
        }

        [Fact]
        public void Load_CacheHit_ReturnsCachedBytesWithoutCallingBuilder()
        {
            // Arrange
            var cache = new FileSystemCertificateCache(_setting);
            var expectedBytes = CreateFakeCertificateBytes(DateTime.UtcNow.AddMonths(6));
            var builderCallCount = 0;

            byte[] Builder(string domain)
            {
                builderCallCount++;
                return expectedBytes;
            }

            // First call - populates cache
            cache.Load("serial123", "example.com", Builder);

            // Act - second call should hit cache
            var result = cache.Load("serial123", "example.com", Builder);

            // Assert
            Assert.Equal(1, builderCallCount); // Builder called only once
            Assert.Equal(expectedBytes, result);
        }

        [Fact]
        public void Load_ExpiredCertificate_RegeneratesCertificate()
        {
            // Arrange
            var cache = new FileSystemCertificateCache(_setting);

            // Write an expired certificate directly to file
            var expiredTicks = DateTime.UtcNow.AddMinutes(-5).Ticks;
            var oldCertBytes = new byte[] { 1, 2, 3, 4 };
            var fileName = Path.Combine(_tempDirectory, "serial123", "example.com.validitychecked.pfx");
            Directory.CreateDirectory(Path.GetDirectoryName(fileName)!);

            using (var stream = File.Create(fileName)) {
                stream.Write(BitConverter.GetBytes(expiredTicks));
                stream.Write(oldCertBytes);
            }

            var newCertBytes = CreateFakeCertificateBytes(DateTime.UtcNow.AddMonths(6));
            var builderCallCount = 0;

            byte[] Builder(string domain)
            {
                builderCallCount++;
                return newCertBytes;
            }

            // Act
            var result = cache.Load("serial123", "example.com", Builder);

            // Assert
            Assert.Equal(1, builderCallCount); // Builder was called because cache was expired
            Assert.Equal(newCertBytes, result);
        }

        [Fact]
        public void Load_ValidCachedCertificate_DoesNotCallBuilder()
        {
            // Arrange
            var cache = new FileSystemCertificateCache(_setting);

            // Write a valid certificate directly to file
            var validTicks = DateTime.UtcNow.AddMonths(6).Ticks;
            var cachedCertBytes = new byte[] { 10, 20, 30, 40, 50 };
            var fileName = Path.Combine(_tempDirectory, "serial123", "example.com.validitychecked.pfx");
            Directory.CreateDirectory(Path.GetDirectoryName(fileName)!);

            using (var stream = File.Create(fileName)) {
                stream.Write(BitConverter.GetBytes(validTicks));
                stream.Write(cachedCertBytes);
            }

            var builderCallCount = 0;

            byte[] Builder(string domain)
            {
                builderCallCount++;
                return CreateFakeCertificateBytes(DateTime.UtcNow.AddMonths(6));
            }

            // Act
            var result = cache.Load("serial123", "example.com", Builder);

            // Assert
            Assert.Equal(0, builderCallCount); // Builder was NOT called
            Assert.Equal(cachedCertBytes, result);
        }

        [Fact]
        public void Load_DisableCertificateCache_AlwaysCallsBuilder()
        {
            // Arrange
            _setting.DisableCertificateCache = true;
            var cache = new FileSystemCertificateCache(_setting);
            var certBytes = CreateFakeCertificateBytes(DateTime.UtcNow.AddMonths(6));
            var builderCallCount = 0;

            byte[] Builder(string domain)
            {
                builderCallCount++;
                return certBytes;
            }

            // Act - call twice
            cache.Load("serial123", "example.com", Builder);
            cache.Load("serial123", "example.com", Builder);

            // Assert
            Assert.Equal(2, builderCallCount); // Builder called each time
        }

        [Fact]
        public void Load_InvalidFileFormat_RegeneratesCertificate()
        {
            // Arrange
            var cache = new FileSystemCertificateCache(_setting);

            // Write a file with invalid format (too short, no header)
            var fileName = Path.Combine(_tempDirectory, "serial123", "example.com.validitychecked.pfx");
            Directory.CreateDirectory(Path.GetDirectoryName(fileName)!);
            File.WriteAllBytes(fileName, new byte[] { 1, 2, 3 }); // Only 3 bytes, less than header size

            var newCertBytes = CreateFakeCertificateBytes(DateTime.UtcNow.AddMonths(6));
            var builderCallCount = 0;

            byte[] Builder(string domain)
            {
                builderCallCount++;
                return newCertBytes;
            }

            // Act
            var result = cache.Load("serial123", "example.com", Builder);

            // Assert
            Assert.Equal(1, builderCallCount);
            Assert.Equal(newCertBytes, result);
        }

        [Fact]
        public void Load_DifferentDomains_CachesSeparately()
        {
            // Arrange
            var cache = new FileSystemCertificateCache(_setting);
            var cert1 = CreateFakeCertificateBytes(DateTime.UtcNow.AddMonths(6));
            var cert2 = CreateFakeCertificateBytes(DateTime.UtcNow.AddMonths(6));
            var builderCallCount = 0;

            byte[] Builder(string domain)
            {
                builderCallCount++;
                return domain == "example.com" ? cert1 : cert2;
            }

            // Act
            var result1 = cache.Load("serial123", "example.com", Builder);
            var result2 = cache.Load("serial123", "other.com", Builder);

            // Assert
            Assert.Equal(2, builderCallCount);
            Assert.Equal(cert1, result1);
            Assert.Equal(cert2, result2);
        }

        [Fact]
        public void Load_DifferentSerialNumbers_CachesSeparately()
        {
            // Arrange
            var cache = new FileSystemCertificateCache(_setting);
            var cert1 = CreateFakeCertificateBytes(DateTime.UtcNow.AddMonths(6));
            var cert2 = CreateFakeCertificateBytes(DateTime.UtcNow.AddMonths(6));
            var callIndex = 0;

            byte[] Builder(string domain)
            {
                return callIndex++ == 0 ? cert1 : cert2;
            }

            // Act
            var result1 = cache.Load("serial1", "example.com", Builder);
            var result2 = cache.Load("serial2", "example.com", Builder);

            // Assert
            Assert.Equal(cert1, result1);
            Assert.Equal(cert2, result2);
        }

        [Fact]
        public void Load_CreatesCorrectFileStructure()
        {
            // Arrange
            var cache = new FileSystemCertificateCache(_setting);
            var certBytes = CreateFakeCertificateBytes(DateTime.UtcNow.AddMonths(6));

            byte[] Builder(string domain) => certBytes;

            // Act
            cache.Load("serial123", "example.com", Builder);

            // Assert
            var fileName = Path.Combine(_tempDirectory, "serial123", "example.com.validitychecked.pfx");
            Assert.True(File.Exists(fileName));

            var fileContent = File.ReadAllBytes(fileName);
            Assert.True(fileContent.Length > ExpirationHeaderSize);

            // Verify header contains valid ticks
            var ticks = BitConverter.ToInt64(fileContent.AsSpan(0, ExpirationHeaderSize));
            var notAfter = new DateTime(ticks, DateTimeKind.Utc);
            Assert.True(notAfter > DateTime.UtcNow);

            // Verify certificate bytes follow header
            var storedCertBytes = fileContent.AsSpan(ExpirationHeaderSize).ToArray();
            Assert.Equal(certBytes, storedCertBytes);
        }

        [Fact]
        public void Load_ExpirationWithinBuffer_RegeneratesCertificate()
        {
            // Arrange
            var cache = new FileSystemCertificateCache(_setting);

            // Write a certificate that expires in 30 seconds (within 1-minute buffer)
            var almostExpiredTicks = DateTime.UtcNow.AddSeconds(30).Ticks;
            var oldCertBytes = new byte[] { 1, 2, 3, 4 };
            var fileName = Path.Combine(_tempDirectory, "serial123", "example.com.validitychecked.pfx");
            Directory.CreateDirectory(Path.GetDirectoryName(fileName)!);

            using (var stream = File.Create(fileName)) {
                stream.Write(BitConverter.GetBytes(almostExpiredTicks));
                stream.Write(oldCertBytes);
            }

            var newCertBytes = CreateFakeCertificateBytes(DateTime.UtcNow.AddMonths(6));
            var builderCallCount = 0;

            byte[] Builder(string domain)
            {
                builderCallCount++;
                return newCertBytes;
            }

            // Act
            var result = cache.Load("serial123", "example.com", Builder);

            // Assert
            Assert.Equal(1, builderCallCount); // Regenerated because within buffer
            Assert.Equal(newCertBytes, result);
        }

        /// <summary>
        /// Creates fake certificate bytes with a valid PKCS12 structure containing the specified NotAfter date.
        /// </summary>
        private static byte[] CreateFakeCertificateBytes(DateTime notAfter)
        {
            // Create a real self-signed certificate to get valid PKCS12 bytes
            using var rsa = System.Security.Cryptography.RSA.Create(2048);
            var request = new System.Security.Cryptography.X509Certificates.CertificateRequest(
                "CN=Test",
                rsa,
                System.Security.Cryptography.HashAlgorithmName.SHA256,
                System.Security.Cryptography.RSASignaturePadding.Pkcs1);

            using var cert = request.CreateSelfSigned(
                DateTimeOffset.UtcNow.AddMinutes(-1),
                new DateTimeOffset(notAfter));

            return cert.Export(System.Security.Cryptography.X509Certificates.X509ContentType.Pkcs12);
        }
    }
}
