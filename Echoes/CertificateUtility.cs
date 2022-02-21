using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Echoes.Core.Utils;

namespace Echoes
{
    public class CertificateUtility
    {
        internal static readonly string DefaultTempPath;

        static CertificateUtility()
        {
            DefaultTempPath =
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Echoes", "Temp");

            Directory.CreateDirectory(DefaultTempPath);
        }

        /// <summary>
        /// Write the default certificate on P12 fORMAT to stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static async Task DumpDefaultCertificate(Stream stream)
        {
            await stream.WriteAsyncNS2(EchoesSecurity.DefaultCertificate.Export(X509ContentType.Cert)).ConfigureAwait(false);
        }
        
        public static bool IsCertificateInstalled(string serialNumber)
        {
            using (X509Store store = new X509Store(StoreName.Root))
            {
                store.Open(OpenFlags.ReadOnly);
                
                var certificates = store.Certificates.Find(X509FindType.FindBySerialNumber, serialNumber, false);
                return certificates.Count > 0;
            }
        }

        public static bool IsDefaultCertificateInstalled()
        {
            return IsCertificateInstalled(EchoesSecurity.DefaultSerialNumber);

        }

        public static bool RemoveCertificate(string serialNumber)
        {
            using (X509Store store = new X509Store(StoreName.Root))
            {
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
        }
        
        public static void InstallCertificate(X509Certificate2 certificate)
        {
            using (var newCertificate = new X509Certificate2(certificate.Export(X509ContentType.Cert)))
            {
                using (X509Store store = new X509Store(StoreName.Root))
                {
                    store.Open(OpenFlags.ReadWrite);
                    store.Add(newCertificate);
                }
            }
        }

        public static void InstallDefaultCertificate()
        {
            InstallCertificate(EchoesSecurity.DefaultCertificate);
        }

        public static void CheckAndInstallCertificate(ProxyStartupSetting startupSetting)
        {
            var certificate = startupSetting.CertificateConfiguration.DefaultConfig
                ? EchoesSecurity.DefaultCertificate
                : startupSetting.CertificateConfiguration.Certificate;

            if (!IsCertificateInstalled(certificate.SerialNumber))
            {
                InstallCertificate(certificate);
            }
        }
    }
}