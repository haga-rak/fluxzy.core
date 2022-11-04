using System.Security.Cryptography.X509Certificates;

namespace Fluxzy
{
    internal static class FluxzySecurity
    {
        static FluxzySecurity()
        {
            DefaultCertificate = new X509Certificate2(FileStore.Fluxzy, "echoes");
        }

        public static X509Certificate2 DefaultCertificate { get;  }

        public static string DefaultSerialNumber => DefaultCertificate.SerialNumber!;
    }
}