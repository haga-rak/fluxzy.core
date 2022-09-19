using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Fluxzy.Core.Utils;

namespace Fluxzy
{
    public class CertificateUtility
    {
        internal static readonly string DefaultTempPath;

        static CertificateUtility()
        {
            DefaultTempPath =
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Fluxzy", "Temp");

            Directory.CreateDirectory(DefaultTempPath);
        }

        /// <summary>
        /// Write the default CA Certificate without private key
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static void DumpDefaultCertificate(Stream stream)
        {
            FluxzySecurity.DefaultCertificate.ExportToPem(stream);
        }
        
        /// <summary>
        /// Check whether a certificate is installed as root certificate
        /// </summary>
        /// <param name="certificateSerialNumber"></param>
        /// <returns></returns>
        public static bool IsCertificateInstalled(string certificateSerialNumber)
        {
            using var store = new X509Store(StoreName.Root);

            store.Open(OpenFlags.ReadOnly);
                
            var certificates = store.Certificates.Find(X509FindType.FindBySerialNumber, certificateSerialNumber, false);
            return certificates.Count > 0;
        }

        public static bool IsDefaultCertificateInstalled()
        {
            return IsCertificateInstalled(FluxzySecurity.DefaultSerialNumber);

        }

        public static bool RemoveCertificate(string serialNumber)
        {
            using X509Store store = new X509Store(StoreName.Root);

            store.Open(OpenFlags.ReadWrite);

            foreach (var certificate in store.Certificates.Find(X509FindType.FindBySerialNumber, serialNumber, false))
            {
                try
                {
                    store.Remove(certificate);
                }
                catch (Exception)
                {
                    return false;
                }
            }

            return true;
        }

        public static void InstallCertificate(X509Certificate2 certificate)
        {
            using var newCertificate = new X509Certificate2(certificate.Export(X509ContentType.Cert));
            using X509Store store = new X509Store(StoreName.Root);

            store.Open(OpenFlags.ReadWrite);
            store.Add(newCertificate);
        }

        public static void InstallDefaultCertificate()
        {
            InstallCertificate(FluxzySecurity.DefaultCertificate);
        }

        public static void CheckAndInstallCertificate(FluxzySetting startupSetting)
        {
            var certificate = startupSetting.CaCertificate.GetCertificate();

            if (!IsCertificateInstalled(certificate.SerialNumber))
            {
                InstallCertificate(certificate);
            }
        }
    }

    public static class CertificateExtension
    {
        public static void ExportToPem(this X509Certificate cert, Stream stream)
        {
            using var streamWriter = new StreamWriter(stream, Encoding.ASCII, 1024 * 8, true);

            streamWriter.NewLine = "\r\n"; 
            streamWriter.WriteLine("-----BEGIN CERTIFICATE-----");
            streamWriter.WriteLine(Convert.ToBase64String(cert.Export(X509ContentType.Cert), Base64FormattingOptions.InsertLineBreaks));
            streamWriter.WriteLine("-----END CERTIFICATE-----");
        }
    }
}