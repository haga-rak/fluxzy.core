// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Fluxzy.Certificates
{
    /// <summary>
    ///     The default certificate authority manager
    /// </summary>
    public class DefaultCertificateAuthorityManager : CertificateAuthorityManager
    {
        private static readonly bool AttemptAskElevation  = string.Equals(Environment.GetEnvironmentVariable("FLUXZY_ASK_ELEVATION"),
            "1", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        ///     Check whether a certificate is installed as root certificate
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        public override bool IsCertificateInstalled(X509Certificate2 certificate)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                return ExtendedMacOsCertificateInstaller.IsCertificateInstalled(certificate, AttemptAskElevation);
            }

            // TODO implementation for Linux should be here 
            
            using var store = new X509Store(StoreName.Root);
            store.Open(OpenFlags.ReadOnly);
            var certificates = store.Certificates.Find(X509FindType.FindByThumbprint, certificate.Thumbprint, false);
            return certificates.Count > 0;
        }

        public override ValueTask<bool> RemoveCertificate(string thumbPrint)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                ExtendedLinuxCertificateInstaller.Uninstall(thumbPrint, AttemptAskElevation);
                return new ValueTask<bool>(true); 
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                // ExtendedMacOsCertificateInstaller.Uninstall(thumbPrint);
                return new ValueTask<bool>(false);
            }

            using var store = new X509Store(StoreName.Root);

            store.Open(OpenFlags.ReadWrite);

            foreach (var certificate in store.Certificates.Find(X509FindType.FindByThumbprint, thumbPrint, false))
            {
                try
                {
                    store.Remove(certificate);

                    //if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    //    ExtendedLinuxCertificateInstaller.Install(certificate);
                }
                catch (Exception)
                {
                    return new ValueTask<bool>(false);
                }
            }

            return new ValueTask<bool>(true);
        }

        public override ValueTask<bool> InstallCertificate(X509Certificate2 certificate)
        {
            try {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    ExtendedLinuxCertificateInstaller.Install(certificate, AttemptAskElevation);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    ExtendedMacOsCertificateInstaller.Install(certificate, AttemptAskElevation);
                
                using var newCertificate = new X509Certificate2(certificate.Export(X509ContentType.Cert));
                using var store = new X509Store(StoreName.Root);

                try {
                    store.Open(OpenFlags.ReadWrite);
                    store.Add(newCertificate);
                }
                catch (Exception) {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        throw; 
                    // we ignore errors for macos and linux
                }
                
                return new ValueTask<bool>(true);
            }
            catch (Exception ex) {
                Console.Error.WriteLineAsync($"Certificate installation failed (possible insufficient right)");
                Console.Error.WriteLineAsync($"Error message : {ex.Message}");
                // Probably user as refused to install the certificate or user has not enough right 
                return new ValueTask<bool>(false);
            }
        }

        public override IEnumerable<CaCertificateInfo> EnumerateRootCertificates()
        {
            using var store = new X509Store(StoreName.Root);

            store.Open(OpenFlags.ReadOnly);

            var result = new List<CaCertificateInfo>();

            foreach (var certificate in store.Certificates)
            {
                result.Add(new CaCertificateInfo(certificate.Thumbprint ?? string.Empty, certificate.Subject));
            }

            return result;
        }
    }

    internal class InstallableCertificate
    {
        public InstallableCertificate(string directory, string updateCommand, string updateCommandArgs)
        {
            Directory = directory;
            UpdateCommand = updateCommand;
            UpdateCommandArgs = updateCommandArgs;
        }

        public string Directory { get; }

        public string UpdateCommand { get; }

        public string UpdateCommandArgs { get; }
    }
}
