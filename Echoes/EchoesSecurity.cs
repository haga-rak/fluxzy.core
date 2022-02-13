using System.Security.Cryptography.X509Certificates;

namespace Echoes
{
    internal static class EchoesSecurity
    {
        static EchoesSecurity()
        {
            DefaultCertificate = new X509Certificate2(FileStore.Echoes, "echoes");
        }

        public static X509Certificate2 DefaultCertificate { get;  }

        public static string DefaultSerialNumber => DefaultCertificate.SerialNumber;
    }
}