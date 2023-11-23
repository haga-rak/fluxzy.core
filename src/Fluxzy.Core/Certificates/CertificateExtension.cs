// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Fluxzy.Certificates
{
    /// <summary>
    /// Utilities to operate on <see cref="X509Certificate"/>
    /// </summary>
    public static class CertificateExtension
    {
        /// <summary>
        /// Export the certificate to a PEM file
        /// </summary>
        /// <param name="cert"></param>
        /// <param name="fileName"></param>
        public static void ExportToPem(this X509Certificate cert, string fileName)
        {
            using var fileStream = File.Create(fileName);
            cert.ExportToPem(fileStream);
        }
        
        /// <summary>
        /// Export the certificate to a PEM stream
        /// </summary>
        /// <param name="cert"></param>
        /// <param name="stream"></param>
        public static void ExportToPem(this X509Certificate cert, Stream stream)
        {
            using var streamWriter = new StreamWriter(stream, Encoding.ASCII, 1024 * 8, true);

            streamWriter.NewLine = "\r\n";
            streamWriter.WriteLine("-----BEGIN CERTIFICATE-----");

            streamWriter.WriteLine(Convert.ToBase64String(cert.Export(X509ContentType.Cert),
                Base64FormattingOptions.InsertLineBreaks));

            streamWriter.WriteLine("-----END CERTIFICATE-----");
        }

        /// <summary>
        /// Export the certificate to a PEM byte array
        /// </summary>
        /// <param name="cert"></param>
        /// <returns></returns>
        public static byte[] ExportToPem(this X509Certificate cert)
        {
            using var memoryStream = new MemoryStream();

            cert.ExportToPem(memoryStream);

            return memoryStream.ToArray();
        }
    }
}
