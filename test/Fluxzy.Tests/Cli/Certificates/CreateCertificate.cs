using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Fluxzy.Tests.Cli.Scaffolding;
using Xunit;

namespace Fluxzy.Tests.Cli.Certificates
{
    public class CreateCertificate : CommandBase
    {
        public CreateCertificate()
            : base("certificate")
        {

        }

        [Fact]
        public async Task Check_Default_Values()
        {
            var getTempPath = GetTempFile();
            await InternalRun("create", getTempPath.FullName, "TestCN");

            var certificate = new X509Certificate2(getTempPath.FullName);

            Assert.True(getTempPath.Exists);
            Assert.Equal(2048, certificate.PublicKey.GetRSAPublicKey()!.KeySize);
            Assert.Equal(10 * 365D, (certificate.NotAfter - DateTime.Now).TotalDays, 1);
            Assert.Equal("CN=TestCN", certificate.Subject);
        }

        [Fact]
        public async Task Check_With_Option()
        {
            var getTempPath = GetTempFile();
            await InternalRun("create", getTempPath.FullName, 
                "TestCN", "-v", (365*5).ToString(), "-k", "1024"
                , "--L", "PARIS", "--C", "FR", "--O", "Fluxzy_ORG", "--OU", "Fluxzy_OU",
                "--ST", "IDF"
                );

            var certificate = new X509Certificate2(getTempPath.FullName);

            Assert.True(getTempPath.Exists);
            Assert.Equal(1024, certificate.PublicKey.GetRSAPublicKey()!.KeySize);
            Assert.Equal(5 * 365D, (certificate.NotAfter - DateTime.Now).TotalDays, 1);
            Assert.Equal("CN=TestCN, L=PARIS, C=FR, O=Fluxzy_ORG, O=IDF, OU=Fluxzy_OU", certificate.Subject);
        }

    }
}
