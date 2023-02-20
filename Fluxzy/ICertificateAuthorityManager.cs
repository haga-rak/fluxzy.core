using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace Fluxzy
{
    public interface ICertificateAuthorityManager
    {
        /// <summary>
        ///     Write the default CA Certificate without private key
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        void DumpDefaultCertificate(Stream stream);

        /// <summary>
        ///     Check whether a certificate is installed as root certificate
        /// </summary>
        /// <param name="certificateSerialNumber"></param>
        /// <returns></returns>
        bool IsCertificateInstalled(string certificateSerialNumber);

        bool IsDefaultCertificateInstalled();
    
        bool RemoveCertificate(string serialNumber);
    
        void InstallCertificate(X509Certificate2 certificate);
    
        void InstallDefaultCertificate();
    
        void CheckAndInstallCertificate(FluxzySetting startupSetting);
    }
}