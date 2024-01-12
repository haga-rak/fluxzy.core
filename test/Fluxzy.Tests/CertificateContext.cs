// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using Fluxzy.Tests._Files;

namespace Fluxzy.Tests
{
    internal static class CertificateContext
    {
        public static void InstallDefaultCertificate()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);

            store.Open(OpenFlags.ReadWrite);

            var certificate = new X509Certificate2(
                StorageContext.client_cert,
                "Multipass85/"
            );

            ThumbPrint = certificate.Thumbprint;
            SerialNumber = certificate.SerialNumber;

            if (store.Certificates.Find(X509FindType.FindByThumbprint, ThumbPrint, false)
                     .Count > 0)
                return; 

            store.Add(certificate);
        }

        public static string SerialNumber { get; set; } = null!;

        public static string ThumbPrint { get; private set; } = null!;

        public static string DefaultPassword = "Multipass85/";
    }
}
