using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Fluxzy.Tests._Files;
using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
using Xunit;

namespace Fluxzy.Tests.Certificates
{
    public class CertificateManager
    {
        private readonly X509Certificate2 _certificate;
        private readonly DefaultCertificateAuthorityManager _manager;

        public CertificateManager()
        {
            
            _certificate = new X509Certificate2("_Files/Certificates/fluxzytest.txt");
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