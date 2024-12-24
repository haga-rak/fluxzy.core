// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.IO;
using Org.BouncyCastle.Tls;

namespace Fluxzy.Clients.Ssl.BouncyCastle
{
    internal static class ConstBuffers
    {
        public static int ExtensionTypeAlps { get; } = 0x4469; 

        public static byte[] EmptyOctet { get; } = new byte[1];

        public static byte[] ClientExtensionsDefaultCompressCertificate { get; } =
            TlsExtensionsUtilities.CreateCompressCertificateExtension(new[] {
                382
            });

        public static byte[] ClientExtensionsPskKeyExchangeModes { get; } = 
            TlsExtensionsUtilities.CreatePskKeyExchangeModesExtension(new short[] { 1 });

        public static byte[] ClientExtensionsDefaultMaxSizeRecordLimit { get; } =
            BinaryUtilities.GetBytesBigEndian(16884);

        public static byte[] ClientExtensionsDummyPadding { get;  } = TlsExtensionsUtilities.CreatePaddingExtension(6);

        public static byte[] Http2ApplicationProtocol { get; } = 
            new byte[] { 0, 0x3, 0x02, 0x68, 0x32 };

        public static byte[] EncryptedExtensionAlpsH2 { get;  }

        static ConstBuffers()
        {
            EncryptedExtensionAlpsH2 = CreateEncryptedExtensionAlpsH2Ok();
        }

        private static byte[] CreateEncryptedExtensionAlpsH2Ok()
        {
            using var memoryStream = new MemoryStream();
            TlsProtocol.WriteExtensions(memoryStream, new Dictionary<int, byte[]>()
            {
                [17513] = Array.Empty<byte>()
            });
            var data = memoryStream.ToArray();

            return data; 
        }
    }
}
