// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Fluxzy.Certificates
{
    public abstract class CertificateAuthorityManager
    {
        /// <summary>
        ///     Write the default CA Certificate without private key
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public virtual void DumpDefaultCertificate(Stream stream)
        {
            FluxzySecurity.BuiltinCertificate.ExportToPem(stream);
        }

        /// <summary>
        ///     Check whether a certificate is installed as root certificate
        /// </summary>
        /// <param name="certificateThumbPrint"></param>
        /// <returns></returns>
        public abstract bool IsCertificateInstalled(string certificateThumbPrint);

        /// <summary>
        ///     Check if the default certificate is installed
        /// </summary>
        /// <returns></returns>
        public virtual bool IsDefaultCertificateInstalled()
        {
            return IsCertificateInstalled(FluxzySecurity.DefaultThumbPrint);
        }

        public abstract ValueTask<bool> RemoveCertificate(string thumbPrint);

        public abstract ValueTask<bool> InstallCertificate(X509Certificate2 certificate);

        public virtual ValueTask<bool> InstallDefaultCertificate()
        {
            return InstallCertificate(FluxzySecurity.BuiltinCertificate);
        }

        public virtual void CheckAndInstallCertificate(FluxzySetting startupSetting)
        {
            var certificate = startupSetting.CaCertificate.GetX509Certificate();

            if (!IsCertificateInstalled(certificate.Thumbprint!))
                InstallCertificate(certificate);
        }

        public abstract IEnumerable<CaCertificateInfo> EnumerateRootCertificates();
    }

    public class CaCertificateInfo
    {
        public CaCertificateInfo(string thumbPrint, string subject)
        {
            ThumbPrint = thumbPrint;
            Subject = subject;
        }

        public string ThumbPrint { get; }

        public string Subject { get; }
    }
}
