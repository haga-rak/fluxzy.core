// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Fluxzy.Certificates
{
    /// <summary>
    /// An utility to create and manage certificate authority
    /// </summary>
    public abstract class CertificateAuthorityManager
    {
        /// <summary>
        ///     Write the default CA Certificate without private key
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public virtual void DumpDefaultCertificate(Stream stream)
        {
            FluxzySecurity.DefaultInstance.BuiltinCertificate.ExportToPem(stream);
        }

        /// <summary>
        ///     Check whether a certificate is installed as root certificate
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        public abstract bool IsCertificateInstalled(X509Certificate2 certificate);

        ///// <summary>
        /////     Check if the default certificate is installed
        ///// </summary>
        ///// <returns></returns>
        //public virtual bool IsDefaultCertificateInstalled()
        //{
        //    return IsCertificateInstalled(FluxzySecurity.DefaultThumbPrint);
        //}

        /// <summary>
        ///    Remove a certificate from the root store
        /// </summary>
        /// <param name="thumbPrint"></param>
        /// <returns></returns>
        public abstract ValueTask<bool> RemoveCertificate(string thumbPrint);

        /// <summary>
        ///    Install a certificate as root certificate
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        public abstract ValueTask<bool> InstallCertificate(X509Certificate2 certificate);

        /// <summary>
        ///    Install the default root CA
        /// </summary>
        /// <returns></returns>
        public virtual ValueTask<bool> InstallDefaultCertificate()
        {
            return InstallCertificate(FluxzySecurity.DefaultInstance.BuiltinCertificate);
        }

        /// <summary>
        /// Check if the provided certificate is installed a root CA
        /// </summary>
        /// <param name="certificate"></param>
        public virtual void CheckAndInstallCertificate(X509Certificate2 certificate)
        {
            if (!IsCertificateInstalled(certificate))
                InstallCertificate(certificate);
        }

        /// <summary>
        /// List all installed root certificates
        /// </summary>
        /// <returns></returns>
        public abstract IEnumerable<CaCertificateInfo> EnumerateRootCertificates();
    }

    /// <summary>
    /// A light information about a CA certificate
    /// </summary>
    public class CaCertificateInfo
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="thumbPrint"></param>
        /// <param name="subject"></param>
        public CaCertificateInfo(string thumbPrint, string subject)
        {
            ThumbPrint = thumbPrint;
            Subject = subject;
        }

        /// <summary>
        /// The certifiate thumbprint 
        /// </summary>
        public string ThumbPrint { get; }

        /// <summary>
        /// The certificate subject 
        /// </summary>
        public string Subject { get; }
    }
}
