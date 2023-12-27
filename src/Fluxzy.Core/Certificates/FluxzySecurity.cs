// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace Fluxzy.Certificates
{
    /// <summary>
    ///  Solve the default root certificate used by fluxzy in the following order:
    ///  - From the path specified in the environment variable FLUXZY_ROOT_CERTIFICATE which must be a path to a PKCS12 File
    ///  - From the static filesystem path %appdata%/.fluxzy/rootca.pfx
    ///  - If none of the above is available, use the built-in certificate
    ///
    ///  For the two first cases, if the PKCS12 file has a password, it must be specified in the environment variable FLUXZY_ROOT_CERTIFICATE_PASSWORD
    /// </summary>
    internal static class FluxzySecurity
    {
        static FluxzySecurity()
        {
            BuiltinCertificate = GetDefaultCertificate();
        }
        
        private static X509Certificate2 GetDefaultCertificate()
        {
            var certificatePath = Environment.GetEnvironmentVariable("FLUXZY_ROOT_CERTIFICATE");
            var certificatePassword = Environment.GetEnvironmentVariable("FLUXZY_ROOT_CERTIFICATE_PASSWORD");

            if (certificatePath != null) {
                if (!File.Exists(certificatePath)) {
                    throw new Exception($"The certificate file {certificatePath} (from FLUXZY_ROOT_CERTIFICATE variable) does not exist");
                }
            }
            else
            {
                var defaultPath = Environment.ExpandEnvironmentVariables("%appdata%/.fluxzy/rootca.pfx");

                if (File.Exists(defaultPath))
                {
                    certificatePath = defaultPath;
                }
            }

            if (certificatePath != null)
            {
                if (certificatePassword == null)
                {
                    return new X509Certificate2(certificatePath);
                }

                return new X509Certificate2(certificatePath, certificatePassword);
            }

            return new X509Certificate2(FileStore.Fluxzy, "youshallnotpass");
        }

        public static X509Certificate2 BuiltinCertificate { get; }

        public static string DefaultThumbPrint => BuiltinCertificate.Thumbprint!;
    }
}
