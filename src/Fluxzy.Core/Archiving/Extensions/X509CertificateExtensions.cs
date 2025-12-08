// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Org.BouncyCastle.Tls;

namespace Fluxzy.Extensions
{
    public static class X509CertificateExtensions
    {
        public static string ToPem(this X509Certificate cert)
        {
            if (cert == null)
                throw new ArgumentNullException(nameof(cert));

            var raw = cert.Export(X509ContentType.Cert);
            var base64 = Convert.ToBase64String(raw, Base64FormattingOptions.InsertLineBreaks);

            var builder = new StringBuilder();
            builder.AppendLine("-----BEGIN CERTIFICATE-----");
            builder.AppendLine(base64);
            builder.AppendLine("-----END CERTIFICATE-----");

            return builder.ToString();
        }

        public static string ToPem(this Certificate tlsCert)
        {
            if (tlsCert == null)
                throw new ArgumentNullException(nameof(tlsCert));

            var sb = new StringBuilder();

            foreach (var entry in tlsCert.GetCertificateList())
            {
                var der = entry.GetEncoded();
                var base64 = Convert.ToBase64String(der, Base64FormattingOptions.InsertLineBreaks);

                sb.AppendLine("-----BEGIN CERTIFICATE-----");
                sb.AppendLine(base64);
                sb.AppendLine("-----END CERTIFICATE-----");
            }

            return sb.ToString();
        }
    }
}
