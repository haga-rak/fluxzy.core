// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Linq;
using System.Runtime.InteropServices;
using Fluxzy.Certificates;
using Xunit;
using Xunit.Sdk;

namespace Fluxzy.Tests.UnitTests.Settings
{
    public class CertificateTests
    {
        [Fact]
        public void Load_Root_Certs()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;

            var defaultAuthorityManager = new DefaultCertificateAuthorityManager();

            var rootCerts = defaultAuthorityManager.EnumerateRootCertificates().ToList();

            Assert.NotEmpty(rootCerts);
        }

        [Fact]
        public void Certificate_Installed()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;

            var defaultAuthorityManager = new DefaultCertificateAuthorityManager();
            var defaultCert = Certificate.UseDefault().GetX509Certificate();

            var result = defaultAuthorityManager.IsCertificateInstalled(defaultCert);

            Assert.True(result);
            
        }
    }
}
