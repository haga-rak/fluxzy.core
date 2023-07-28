// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Concurrent;
using System.IO;

namespace Fluxzy.Core
{
    public class FileSystemCertificateCache : ICertificateCache
    {
        private readonly string _baseDirectory;
        private readonly FluxzySetting _startupSetting;

        public FileSystemCertificateCache(FluxzySetting startupSetting)
        {
            _startupSetting = startupSetting;
            _baseDirectory = Environment.ExpandEnvironmentVariables(startupSetting.CertificateCacheDirectory);
        }

        public byte[] Load(
            string baseCertificatSerialNumber,
            string rootDomain,
            Func<string, byte[]> certificateGeneratoringProcess)
        {
            if (_startupSetting.DisableCertificateCache)
                return certificateGeneratoringProcess(rootDomain);

            var fullFileName = GetCertificateFileName(baseCertificatSerialNumber, rootDomain);

            if (File.Exists(fullFileName))
                return File.ReadAllBytes(fullFileName);

            var fileContent = certificateGeneratoringProcess(rootDomain);
            var containingDirectory = Path.GetDirectoryName(fullFileName);

            if (containingDirectory != null)
                Directory.CreateDirectory(containingDirectory);

            File.WriteAllBytes(fullFileName, fileContent);

            return fileContent;
        }

        private string GetCertificateFileName(string baseSerialNumber, string rootDomain)
        {
            return Path.Combine(_baseDirectory, baseSerialNumber, rootDomain + ".pfx");
        }
    }

    public class InMemoryCertificateCache : ICertificateCache
    {
        private readonly ConcurrentDictionary<string, byte[]> _repository = new();

        public byte[] Load(
            string baseCertificatSerialNumber,
            string rootDomain, Func<string, byte[]> certificateGeneratoringProcess)
        {
            var key = $"{baseCertificatSerialNumber}_{rootDomain}";

            return _repository.GetOrAdd(key, _ =>
                certificateGeneratoringProcess(rootDomain)
            );
        }
    }
}
