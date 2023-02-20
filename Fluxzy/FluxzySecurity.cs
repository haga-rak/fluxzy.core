using System.Security.Cryptography.X509Certificates;

namespace Fluxzy
{
    internal static class FluxzySecurity
    {
        static FluxzySecurity()
        {
            BuiltinCertificate = new X509Certificate2(FileStore.Fluxzy, "echoes");
        }

        public static X509Certificate2 BuiltinCertificate { get;  }

        public static string DefaultSerialNumber => BuiltinCertificate.SerialNumber!;
    }
}