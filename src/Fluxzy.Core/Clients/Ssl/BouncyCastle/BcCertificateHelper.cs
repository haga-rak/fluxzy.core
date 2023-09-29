// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Org.BouncyCastle.Tls.Crypto;
using Org.BouncyCastle.Tls.Crypto.Impl.BC;

namespace Fluxzy.Clients.Ssl.BouncyCastle
{
    internal static class BcCertificateHelper
    {
        public static bool ReadInfo(TlsCertificate certificate, out string? subject, out string? issuer)
        {
            subject = issuer = null;

            if (!(certificate is BcTlsCertificate bcTlsCertificate))
                return false;

            subject = bcTlsCertificate.X509CertificateStructure.Subject.ToString();
            issuer = bcTlsCertificate.X509CertificateStructure.Issuer.ToString();

            return true;
        }

        public static bool ReadInfo(
            Org.BouncyCastle.Tls.Certificate? certificate, out string? subject, out string? issuer)
        {
            subject = issuer = null;

            if (certificate == null || certificate.Length == 0)
                return false;

            var cert = certificate.GetCertificateAt(0);

            return ReadInfo(cert, out subject, out issuer);
        }
    }
}
