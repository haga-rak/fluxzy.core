using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Fluxzy.Tests._Files;
using Xunit;

namespace Fluxzy.Tests.Certificates
{
    public class CertificateManager
    {
        private readonly X509Certificate2 _certificate;
        private readonly DefaultCertificateAuthorityManager _manager;

        public CertificateManager()
        {
            _certificate = new X509Certificate2(StorageContext.fluxzytest);
            _manager = new DefaultCertificateAuthorityManager();
        }
        
        [Fact]
        public async Task IsCertificateInstalled()
        {
            await _manager.InstallCertificate(_certificate); 
            
            Assert.True(_manager.IsCertificateInstalled(_certificate.Thumbprint!)); 
        }
    }


}