using System;
using System.Collections.Concurrent;
using System.IO;

namespace Fluxzy.Core
{
    public class FileSystemCertificateCache : ICertificateCache
    {
        private readonly ProxyStartupSetting _startupSetting;
        private readonly string _baseDirectory;

        public FileSystemCertificateCache(ProxyStartupSetting startupSetting)
        {
            _startupSetting = startupSetting;
            _baseDirectory = Environment.ExpandEnvironmentVariables(startupSetting.CertificateCacheDirectory);
        }

        private string GetCertificateFileName(string baseSerialNumber, string rootDomain)
        {
            return Path.Combine(_baseDirectory, baseSerialNumber, rootDomain + ".crt"); 
        }

        public byte[] Load(
            string baseCertificatSerialNumber, 
            string rootDomain,
            Func<string, byte[]> certificateGeneratoringProcess)
        {
            if (_startupSetting.DisableCertificateCache)
            {
                return certificateGeneratoringProcess(rootDomain);
            }

            var fullFileName = GetCertificateFileName(baseCertificatSerialNumber, rootDomain);

            if (File.Exists(fullFileName))
                return File.ReadAllBytes(fullFileName);

            var fileContent = certificateGeneratoringProcess(rootDomain);
            var containingDirectory = Path.GetDirectoryName(fullFileName);

            if (containingDirectory != null)
            {
                Directory.CreateDirectory(containingDirectory);
            }

            File.WriteAllBytes(fullFileName, fileContent);

            return fileContent;
        }
    }

    public class InMemoryCertificateCache: ICertificateCache
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