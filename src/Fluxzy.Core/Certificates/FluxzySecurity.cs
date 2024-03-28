// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Fluxzy.Core;

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
    internal class FluxzySecurity
    {
        private static readonly string DefaultCertificatePath = "%appdata%/.fluxzy/rootca.pfx";
        private readonly string _certificatePath;
        private readonly EnvironmentProvider _environmentProvider;

        public static readonly FluxzySecurity DefaultInstance = new FluxzySecurity(DefaultCertificatePath, new SystemEnvironmentProvider());
        
        public FluxzySecurity(string certificatePath, EnvironmentProvider environmentProvider)
        {
            _certificatePath = certificatePath;
            _environmentProvider = environmentProvider;
            BuiltinCertificate = GetDefaultCertificate();

            if (!BuiltinCertificate.HasPrivateKey)
            {
                throw new ArgumentException("The built-in certificate must have a private key");
            }
        }

        public X509Certificate2 BuiltinCertificate { get; }

        private X509Certificate2 GetDefaultCertificate()
        {
            var certificatePath = _environmentProvider.GetEnvironmentVariable("FLUXZY_ROOT_CERTIFICATE");
            var certificatePassword = _environmentProvider.GetEnvironmentVariable("FLUXZY_ROOT_CERTIFICATE_PASSWORD");

            if (certificatePath != null) {
                if (!File.Exists(certificatePath)) {
                    throw new Exception($"The certificate file {certificatePath} (from FLUXZY_ROOT_CERTIFICATE variable) does not exist");
                }
            }
            else
            {
                var defaultPath = _environmentProvider.ExpandEnvironmentVariables(_certificatePath);

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

        public void SetDefaultCertificateForUser(byte[] certificateContent)
        {
            var certificateFileInfo = new FileInfo(_environmentProvider.ExpandEnvironmentVariables(_certificatePath));
            var certificateDirectory = certificateFileInfo.Directory;

            if (certificateDirectory != null) {
                Directory.CreateDirectory(certificateDirectory.FullName);
            }

            File.WriteAllBytes(certificateFileInfo.FullName, certificateContent);
        }
    }
}
