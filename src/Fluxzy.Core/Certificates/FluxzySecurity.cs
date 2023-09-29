// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Security.Cryptography.X509Certificates;
using Fluxzy.Core;

namespace Fluxzy.Certificates
{
    internal static class FluxzySecurity
    {
        static FluxzySecurity()
        {
            BuiltinCertificate = new X509Certificate2(FileStore.Fluxzy, "youshallnotpass");
        }

        public static X509Certificate2 BuiltinCertificate { get; }

        public static string DefaultThumbPrint => BuiltinCertificate.Thumbprint!;
    }
}