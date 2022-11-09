// // Copyright 2022 - Haga Rakotoharivelo
// 

using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace Fluxzy.Misc.IpUtils
{
    public static class X509CustomExtensions
    {
        public static bool IsCa(this X509Certificate2 certificate)
        {
            return certificate.Extensions
                              .OfType<X509BasicConstraintsExtension>()
                              .Any(t => t.CertificateAuthority);
        }
    }
}