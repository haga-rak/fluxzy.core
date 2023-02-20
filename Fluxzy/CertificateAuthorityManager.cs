using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace Fluxzy
{
    public class CertificateAuthorityManager : ICertificateAuthorityManager
    {
        private readonly string DefaultTempPath;

        public CertificateAuthorityManager()
        {
            DefaultTempPath =
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Fluxzy", "Temp");

            Directory.CreateDirectory(DefaultTempPath);
        }

        /// <summary>
        ///     Write the default CA Certificate without private key
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public void DumpDefaultCertificate(Stream stream)
        {
            FluxzySecurity.BuiltinCertificate.ExportToPem(stream);
        }

        /// <summary>
        ///     Check whether a certificate is installed as root certificate
        /// </summary>
        /// <param name="certificateSerialNumber"></param>
        /// <returns></returns>
        public bool IsCertificateInstalled(string certificateSerialNumber)
        {
            using var store = new X509Store(StoreName.Root);

            store.Open(OpenFlags.ReadOnly);

            var certificates = store.Certificates.Find(X509FindType.FindBySerialNumber, certificateSerialNumber, false);
            return certificates.Count > 0;
        }

        public bool IsDefaultCertificateInstalled()
        {
            return IsCertificateInstalled(FluxzySecurity.DefaultSerialNumber);
        }

        public bool RemoveCertificate(string serialNumber)
        {
            using var store = new X509Store(StoreName.Root);

            store.Open(OpenFlags.ReadWrite);

            foreach (var certificate in store.Certificates.Find(X509FindType.FindBySerialNumber, serialNumber, false))
                try {
                    store.Remove(certificate);
                }
                catch (Exception) {
                    return false;
                }

            return true;
        }

        public void InstallCertificate(X509Certificate2 certificate)
        {
            using var newCertificate = new X509Certificate2(certificate.Export(X509ContentType.Cert));
            using var store = new X509Store(StoreName.Root);

            store.Open(OpenFlags.ReadWrite);
            store.Add(newCertificate);
        }

        public void InstallDefaultCertificate()
        {
            InstallCertificate(FluxzySecurity.BuiltinCertificate);
        }

        public void CheckAndInstallCertificate(FluxzySetting startupSetting)
        {
            var certificate = startupSetting.CaCertificate.GetCertificate();

            if (!IsCertificateInstalled(certificate.SerialNumber!))
                InstallCertificate(certificate);
        }
    }
}