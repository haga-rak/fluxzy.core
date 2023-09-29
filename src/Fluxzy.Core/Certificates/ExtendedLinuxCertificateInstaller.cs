// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Fluxzy.Misc;

namespace Fluxzy.Certificates
{
    /// <summary>
    ///     Utility for installing certificates on certain Linux distributions
    /// </summary>
    public static class ExtendedLinuxCertificateInstaller
    {
        private static IReadOnlyCollection<InstallableCertificate> InstallableCertificates { get; } = new[] {
            new InstallableCertificate("/usr/local/share/ca-certificates", "update-ca-certificates", string.Empty),
            new InstallableCertificate("/etc/pki/ca-trust/source/anchors", "update-ca-trust", string.Empty)
        };

        public static bool Install(X509Certificate2 x509Certificate2)
        {
            // Extension must be .crt for certain Linux distributions
            var fileName = $"fluxzy-{x509Certificate2.Thumbprint}.crt";

            foreach (var installableCertificate in InstallableCertificates) {
                if (!Directory.Exists(installableCertificate.Directory))
                    continue;

                var filePath = Path.Combine(installableCertificate.Directory, fileName);
                x509Certificate2.ExportToPem(filePath);

                var result = ProcessUtils.QuickRun($"{installableCertificate.UpdateCommand}",
                    $"{installableCertificate.UpdateCommandArgs}");

                if (result.ExitCode == 0)
                    return true;
            }

            return false;
        }

        public static void Uninstall(X509Certificate2 x509Certificate2)
        {
            foreach (var installableCertificate in InstallableCertificates) {
                var filePath = Path.Combine(installableCertificate.Directory,
                    $"fluxzy-{x509Certificate2.Thumbprint}.crt");

                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
        }
    }
}
