// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Fluxzy.Certificates;
using Fluxzy.Cli.System;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Certificates
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

        [Fact]
        public void InstallCertificateAndWaitForResult()
        {
            var outOfProcCertManager = new OutOfProcAuthorityManager();
            Assert.False(_manager.IsCertificateInstalled(_certificate));
            Assert.False(outOfProcCertManager.IsCertificateInstalled(_certificate));
        }

        [Fact]
        public void ResolveRootStoreLocation_UsesSystemBundleOnLinux()
        {
            Assert.Equal(StoreLocation.LocalMachine,
                DefaultCertificateAuthorityManager.ResolveRootStoreLocation(p => p == OSPlatform.Linux));
        }

        [Fact]
        public void ResolveRootStoreLocation_UsesCurrentUserOffLinux()
        {
            Assert.Equal(StoreLocation.CurrentUser,
                DefaultCertificateAuthorityManager.ResolveRootStoreLocation(p => p == OSPlatform.Windows));

            Assert.Equal(StoreLocation.CurrentUser,
                DefaultCertificateAuthorityManager.ResolveRootStoreLocation(p => p == OSPlatform.OSX));
        }

        [Fact]
        public void PerUserRootStore_IsNotReportedAsInstalled_OnLinux()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return;

            using var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);

            using var publicCert = new X509Certificate2(_certificate.Export(X509ContentType.Cert));
            store.Add(publicCert);

            try {
                // The per-user store is invisible to the OS, so trust must be read from LocalMachine\Root only
                Assert.False(_manager.IsCertificateInstalled(_certificate));
            }
            finally {
                store.Remove(publicCert);
            }
        }
    }
}
