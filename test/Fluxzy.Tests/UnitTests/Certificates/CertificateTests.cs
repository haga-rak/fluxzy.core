// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Runtime.InteropServices;
using Fluxzy.Certificates;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Certificates
{
    public class CertificateTests
    {
        [Fact]
        public void LoadFromUserStoreBySerialNumber()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                return;
            }

            var cert = Certificate.LoadFromUserStoreBySerialNumber(CertificateContext.SerialNumber);

            Assert.NotNull(cert);
            Assert.NotNull(cert.GetX509Certificate());
        }

        [Fact]
        public void LoadFromUserStoreBySerialNumber_Fail()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            Assert.Throws<FluxzyException>(() => {
                Certificate.LoadFromUserStoreBySerialNumber("invalid_sn").GetX509Certificate();
            });
        }

        [Fact]
        public void LoadFromUserStoreByThumbprint()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                return;
            }

            var cert = Certificate.LoadFromUserStoreByThumbprint(CertificateContext.ThumbPrint);

            Assert.NotNull(cert);
            Assert.NotNull(cert.GetX509Certificate());
        }

        [Fact]
        public void LoadFromUserStoreByThumbprint_Fail()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                return;
            }
            
            Assert.Throws<FluxzyException>(() => {
                Certificate.LoadFromUserStoreByThumbprint("invalid_thumb_print").GetX509Certificate();
            });
        }

        [Fact]
        public void LoadFromPkcs12()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                return;
            }

            var cert = Certificate.LoadFromPkcs12("_Files/Certificates/client-cert.pifix", "Multipass85/");

            Assert.NotNull(cert);
            Assert.NotNull(cert.GetX509Certificate());
        }
    }
}
