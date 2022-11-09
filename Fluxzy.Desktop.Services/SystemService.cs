// // Copyright 2022 - Haga Rakotoharivelo
// 

using System.Security.Cryptography.X509Certificates;
using Fluxzy.Desktop.Services.Models;
using Fluxzy.Misc.IpUtils;

namespace Fluxzy.Desktop.Services
{
    /// <summary>
    /// Any operation OS related
    /// </summary>
    public class SystemService
    {
        public List<CertificateOnStore> GetStoreCertificates(bool caOnly)
        {
            var result = new List<CertificateOnStore>();

            using var store = new X509Store(StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);

            var listOfCertificates = store.Certificates;

            foreach (var certificate in listOfCertificates) {

                if (!certificate.HasPrivateKey)
                    continue;

                if (caOnly && !certificate.IsCa())
                    continue;
                
                result.Add(new CertificateOnStore(certificate.Thumbprint, certificate.SubjectName.Name.ToString()));
            }

            return result;
        }
    }
}