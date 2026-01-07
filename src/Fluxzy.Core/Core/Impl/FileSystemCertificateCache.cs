// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace Fluxzy.Core
{
    public class FileSystemCertificateCache : ICertificateCache
    {
        /// <summary>
        ///     Buffer time before actual expiration to trigger regeneration.
        /// </summary>
        private static readonly TimeSpan ExpirationBuffer = TimeSpan.FromMinutes(1);

        /// <summary>
        ///     Size of the expiration header (int64 ticks = 8 bytes).
        /// </summary>
        private const int ExpirationHeaderSize = sizeof(long);

        private readonly string _baseDirectory;
        private readonly FluxzySetting _startupSetting;

        public FileSystemCertificateCache(FluxzySetting startupSetting)
        {
            _startupSetting = startupSetting;
            _baseDirectory = Environment.ExpandEnvironmentVariables(startupSetting.CertificateCacheDirectory);
        }

        public byte[] Load(
            string baseCertificateSerialNumber,
            string rootDomain,
            Func<string, byte[]> certificateBuilder)
        {
            if (_startupSetting.DisableCertificateCache)
                return certificateBuilder(rootDomain);

            var fullFileName = GetCertificateFileName(baseCertificateSerialNumber, rootDomain);

            if (File.Exists(fullFileName)) {
                using var stream = File.OpenRead(fullFileName);

                if (stream.Length > ExpirationHeaderSize) {
                    Span<byte> header = stackalloc byte[ExpirationHeaderSize];

                    if (stream.Read(header) == ExpirationHeaderSize) {
                        var ticks = BitConverter.ToInt64(header);
                        var notAfter = new DateTime(ticks, DateTimeKind.Utc);

                        if (notAfter > DateTime.UtcNow.Add(ExpirationBuffer)) {
                            // Still valid, read certificate bytes
                            var certBytes = new byte[stream.Length - ExpirationHeaderSize];
                            stream.ReadExactly(certBytes);
                            return certBytes;
                        }
                    }
                }

                stream.Close();

                // Expired or invalid, delete
                File.Delete(fullFileName);
            }

            // Generate new certificate
            var certContent = certificateBuilder(rootDomain);
            var notAfterDate = GetCertificateNotAfter(certContent);

            var directory = Path.GetDirectoryName(fullFileName);

            if (directory != null)
                Directory.CreateDirectory(directory);

            // Write header (8 bytes ticks) + certificate bytes
            using (var stream = File.Create(fullFileName)) {
                Span<byte> header = stackalloc byte[ExpirationHeaderSize];
                BitConverter.TryWriteBytes(header, notAfterDate.Ticks);
                stream.Write(header);
                stream.Write(certContent);
            }

            return certContent;
        }

        private static DateTime GetCertificateNotAfter(byte[] certificateBytes)
        {
            using var cert = new X509Certificate2(certificateBytes);
            return cert.NotAfter;
        }

        private string GetCertificateFileName(string baseSerialNumber, string rootDomain)
        {
            return Path.Combine(_baseDirectory, baseSerialNumber, $"{rootDomain}.validitychecked.pfx");
        }
    }

    public class InMemoryCertificateCache : ICertificateCache
    {
        /// <summary>
        ///     Buffer time before actual expiration to trigger regeneration.
        /// </summary>
        private static readonly TimeSpan ExpirationBuffer = TimeSpan.FromMinutes(1);

        private readonly ConcurrentDictionary<string, CertificateCacheEntry> _repository = new();

        public byte[] Load(
            string baseCertificateSerialNumber,
            string rootDomain, Func<string, byte[]> certificateBuilder)
        {
            var key = $"{baseCertificateSerialNumber}_{rootDomain}";

            while (true) {
                if (_repository.TryGetValue(key, out var entry)) {
                    if (entry.NotAfter > DateTime.UtcNow.Add(ExpirationBuffer))
                        return entry.Bytes;

                    // Expired, try to remove and regenerate
                    _repository.TryRemove(key, out _);
                }

                var bytes = certificateBuilder(rootDomain);
                var notAfter = GetCertificateNotAfter(bytes);
                var newEntry = new CertificateCacheEntry(bytes, notAfter);

                if (_repository.TryAdd(key, newEntry))
                    return bytes;

                // Another thread added an entry, loop back to check it
            }
        }

        private static DateTime GetCertificateNotAfter(byte[] certificateBytes)
        {
            using var cert = new X509Certificate2(certificateBytes);
            return cert.NotAfter;
        }

        private readonly record struct CertificateCacheEntry(byte[] Bytes, DateTime NotAfter);
    }
}
