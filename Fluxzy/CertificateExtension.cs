// // Copyright 2022 - Haga Rakotoharivelo
// 

using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Fluxzy
{
    public static class CertificateExtension
    {
        public static void ExportToPem(this X509Certificate cert, Stream stream)
        {
            using var streamWriter = new StreamWriter(stream, Encoding.ASCII, 1024 * 8, true);

            streamWriter.NewLine = "\r\n";
            streamWriter.WriteLine("-----BEGIN CERTIFICATE-----");
            streamWriter.WriteLine(Convert.ToBase64String(cert.Export(X509ContentType.Cert),
                Base64FormattingOptions.InsertLineBreaks));
            streamWriter.WriteLine("-----END CERTIFICATE-----");
        }
        public static byte[] ExportToPem(this X509Certificate cert)
        {
            using var memoryStream = new MemoryStream(); 

            using var streamWriter = new StreamWriter(memoryStream, Encoding.ASCII, 1024 * 8, true);

            streamWriter.NewLine = "\r\n";
            streamWriter.WriteLine("-----BEGIN CERTIFICATE-----");
            streamWriter.WriteLine(Convert.ToBase64String(cert.Export(X509ContentType.Cert),
                Base64FormattingOptions.InsertLineBreaks));
            streamWriter.WriteLine("-----END CERTIFICATE-----");

            streamWriter.Flush();

            return memoryStream.ToArray(); 
        }
    }
}