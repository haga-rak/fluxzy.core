// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.IO;
using System.Security.Cryptography.X509Certificates;
using Fluxzy.Misc;

namespace Fluxzy.Certificates
{
    public static class ExtendedMacOsCertificateInstaller
    {
        /// <summary>
        /// Check if certificate is installed with : security verify-cert -c
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        public static bool IsCertificateInstalled(X509Certificate2 certificate)
        {
            var tempFile = Path.GetTempFileName();

            try {

                certificate.ExportToPem(tempFile);
                var runResult = ProcessUtils.QuickRun("security", $"verify-cert -c \"{tempFile}\"");
                return runResult.ExitCode == 0;
            }
            finally {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        /// <summary>
        /// Install certificate with : security add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain r.cer
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        public static bool Install(X509Certificate2 certificate)
        {
            var tempFile = Path.GetTempFileName();

            try {
                certificate.ExportToPem(tempFile);

                var runResult = ProcessUtils.QuickRun("security",
                    $"add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain \"{tempFile}\"");

                return runResult.ExitCode == 0;
            }
            finally {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }
    }
}
