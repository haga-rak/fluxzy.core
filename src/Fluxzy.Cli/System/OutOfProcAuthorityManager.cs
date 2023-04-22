// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Fluxzy.Certificates;
using Fluxzy.Misc;

namespace Fluxzy.Cli.System
{
    /// <summary>
    ///     This class provides features to allow running Fluxzy sudo operation in a separate process
    /// </summary>
    public class OutOfProcAuthorityManager : DefaultCertificateAuthorityManager
    {
        private readonly string _currentBinaryFullPath;
        private readonly DefaultCertificateAuthorityManager _defaultCertificateAuthorityManager;

        public OutOfProcAuthorityManager(DefaultCertificateAuthorityManager defaultCertificateAuthorityManager)
        {
            _defaultCertificateAuthorityManager = defaultCertificateAuthorityManager;
            
            _currentBinaryFullPath = new FileInfo(Assembly.GetExecutingAssembly().Location).FullName;
        }

        public override async ValueTask<bool> RemoveCertificate(string thumbPrint)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                await ProcessUtils.QuickRunAsync($"{_currentBinaryFullPath.RemoveFileExtension()}",
                    $" cert uninstall {thumbPrint}");
                
                // We are using pkexec for linux 

                var result = await ProcessUtils.QuickRunAsync("pkexec",
                    $"{_currentBinaryFullPath.RemoveFileExtension()} cert uninstall {thumbPrint}");

                return result.ExitCode == 0;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                // We are using runas for windows 

                var result = await ProcessUtils.QuickRunAsync("runas",
                    $"/user:Administrator uninstall {thumbPrint}");

                return result.ExitCode == 0;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                var result = await ProcessUtils.QuickRunAsync("pkexec",
                    $"{_currentBinaryFullPath.RemoveFileExtension()} cert uninstall {thumbPrint}");

                return result.ExitCode == 0;
            }

            throw new PlatformNotSupportedException();
        }

        public override async ValueTask<bool> InstallCertificate(X509Certificate2 certificate)
        {
            var buffer = new byte [8 * 1024];
            var memoryStream = new MemoryStream(buffer);

            certificate.ExportToPem(memoryStream);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                // installing certificate for current user

                // Working code for fedora 
                // cp yo.pem /etc/pki/ca-trust/source/anchors/yo.pem
                // update-ca-trust 
                // /usr/local/share/ca-certificates --> ubuntu (must be crt extension) 
                // update-ca-certificates


                // Install for current user 

                await ProcessUtils.QuickRunAsync(
                    $"{_currentBinaryFullPath.RemoveFileExtension()}", "cert install",
                    new MemoryStream(buffer, 0, (int) memoryStream.Position));

                // Install for root 

                var result = await ProcessUtils.QuickRunAsync("pkexec",
                    $"\"{_currentBinaryFullPath.RemoveFileExtension()}\" cert install",
                    new MemoryStream(buffer, 0, (int) memoryStream.Position));

                return result.ExitCode == 0;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                // We are using runas for windows 

                throw new NotSupportedException("Out of proc installation is not supported on windows."); 
            }

            // Adding root certificate on macos s
            // sudo security add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain r.cer 

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                var tempFile = Path.GetTempFileName();

                await File.WriteAllBytesAsync(tempFile, buffer);

                try {
                    var result = await ProcessUtils.QuickRunAsync("security",
                        $"add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain {tempFile}");

                    if (result.ExitCode != 0)
                        return false;
                }
                finally {
                    File.Delete(tempFile);
                }

                return false; 
            }

            throw new PlatformNotSupportedException();
        }
    }

    internal static class RemoveFileExtensions
    {
        public static string RemoveFileExtension(this string fileName)
        {
            // remove file extension and return 

            var tab = fileName.Split(".");

            return string.Join(".", tab[..^1]);
        }
    }
    
}
