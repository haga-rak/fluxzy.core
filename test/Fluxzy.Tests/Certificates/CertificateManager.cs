// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Fluxzy.Certificates;
using Fluxzy.Cli.System;
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

            _manager.RemoveCertificate(_certificate.Thumbprint);
        }

        // [Fact]
        public async Task InstallCertificateAndWaitForResult()
        {
            var outOfProcCertManager = new OutOfProcAuthorityManager(_manager);

            await outOfProcCertManager.InstallCertificate(_certificate);
            Assert.True(_manager.IsCertificateInstalled(_certificate));
        }
    }
}
