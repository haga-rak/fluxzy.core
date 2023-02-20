using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Fluxzy.Misc;
using NotImplementedException = System.NotImplementedException;

namespace Fluxzy.Cli.System
{
    /// <summary>
    /// This class provides features to allow running Fluxzy sudo operation in a separate process
    /// </summary>
    public class OutOfProcAuthorityManager: DefaultCertificateAuthorityManager
    {
        private readonly DefaultCertificateAuthorityManager _defaultCertificateAuthorityManager;
        private readonly string _currentBinaryFullPath;

        public OutOfProcAuthorityManager(DefaultCertificateAuthorityManager defaultCertificateAuthorityManager)
        {
            _defaultCertificateAuthorityManager = defaultCertificateAuthorityManager;
            _currentBinaryFullPath = new FileInfo(Assembly.GetExecutingAssembly().Location).FullName;
        }

        public override async ValueTask<bool> RemoveCertificate(string thumbPrint)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                // We are using pkexec for linux 
                
                var result = await ProcessUtils.QuickRunAsync("pkexec",  
                    $"{_currentBinaryFullPath} uninstall {thumbPrint}");

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
                    $"{_currentBinaryFullPath} uninstall {thumbPrint}");

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
                // We are using pkexec for linux 
                
                var result = await ProcessUtils.QuickRunAsync("pkexec",  
                    $"{_currentBinaryFullPath} install",
                    new MemoryStream(buffer, 0, (int) memoryStream.Position ));

                return result.ExitCode == 0; 
            }
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                // We are using runas for windows 
                
                var result = await ProcessUtils.QuickRunAsync("runas",  
                    $"/user:Administrator {_currentBinaryFullPath} install",
                    new MemoryStream(buffer, 0, (int) memoryStream.Position ));

                return result.ExitCode == 0; 
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                var result = await ProcessUtils.QuickRunAsync("pkexec",  
                    $"{_currentBinaryFullPath} install",
                    new MemoryStream(buffer, 0, (int) memoryStream.Position ));

                return result.ExitCode == 0; 
            }

            throw new PlatformNotSupportedException();
        }
    }
}