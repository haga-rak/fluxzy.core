// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Org.BouncyCastle.Tls;

namespace Fluxzy.Clients.Ssl.BouncyCastle
{
    public class FingerPrintTlsExtensionsEnforcer
    {
        internal static readonly HashSet<int> UnsupportedClientExtensions = new() { 34 };

        public IDictionary<int, byte[]> PrepareExtensions(IDictionary<int, byte[]> current,
            Ja3FingerPrint fingerPrint, string targetHost, ProtocolVersion[] protocolVersions)
        {
            var clientExtensionTypes = fingerPrint.EffectiveClientExtensions;

            var missing = current.Where(c => !clientExtensionTypes.Contains(c.Key))
                                 .Select(s => s.Key).ToList();

            // Remove 
            foreach (var type in missing)
            {
                current.Remove(type);
            }

            // Add or replace

            foreach (var type in clientExtensionTypes)
            {
                if (current.ContainsKey(type))
                {
                    continue; // No need to replace
                }

                var extensionData = GetDefaultClientValueExtension(type, targetHost, protocolVersions);

                if (extensionData == null)
                    continue;

                current.Add(type, extensionData);

                //current[type] = extensionData ??
                //                throw new InvalidOperationException($"Unhandled extension {type}");
            }

            if (fingerPrint.GreaseMode) {
                // generate grease empty ECH
                current[65037] = GreaseEchExtension.GetGreaseEncryptedClientHello();
            }

            return current;
        }
        
        internal byte[]? GetDefaultClientValueExtension(
            int type,
            string targetHost, ProtocolVersion[] protocolVersions)
        {
            if (type == 0)
                return ServerNameUtilities.CreateFromHost(targetHost);

            if (type == ExtensionType.renegotiation_info)
                return ConstBuffers.EmptyOctet;

            if (type == ExtensionType.signed_certificate_timestamp)
                return Array.Empty<byte>();

            if (type == 56026)
                return Array.Empty<byte>();

            if (type == ExtensionType.extended_master_secret)
                return Array.Empty<byte>();

            if (type == ExtensionType.compress_certificate)
                return ConstBuffers.ClientExtensionsDefaultCompressCertificate;

            if (type == ExtensionType.session_ticket)
                return Array.Empty<byte>();

            if (type == ExtensionType.record_size_limit)
                return ConstBuffers.ClientExtensionsDefaultMaxSizeRecordLimit;

            if (type == ExtensionType.padding)
                return ConstBuffers.ClientExtensionsDummyPadding;

            if (type == ExtensionType.psk_key_exchange_modes)
                return ConstBuffers.ClientExtensionsPskKeyExchangeModes;

            if (type == ExtensionType.supported_versions)
                return TlsExtensionsUtilities.CreateSupportedVersionsExtensionClient(protocolVersions);

            if (type == ConstBuffers.ExtensionTypeAlps) // APPLICATION PROTOCOLS 17513 --> https://chromestatus.com/feature/5149147365900288
                return ConstBuffers.Http2ApplicationProtocol;

            if (type == 51) // For TLS 1.2, key_share is not supported but some client may send it with an empty value
                return Array.Empty<byte>();

            if (type == 34) // For TLS 1.2, key_share is not supported but some client may send it with an empty value
                return new byte[] { 0, 8, 04, 03, 05, 03, 06, 03, 02, 03 };

            if (UnsupportedClientExtensions.Contains(type))
                throw new InvalidOperationException($"Unsupported TLS client extension {type}");

            return null; 
        }

        public IEnumerable<(short HandshakeType, byte[] Data)> GetAdditionalExtensions(
            IDictionary<int, byte[]> serverExtensions)
        {
            if (serverExtensions.ContainsKey(ConstBuffers.ExtensionTypeAlps))
            {
                yield return (HandshakeType.encrypted_extensions,
                    ConstBuffers.EncryptedExtensionAlpsH2);
            }
        }
    }

    internal static class BinaryUtilities
    {
        public static byte[] GetBytesBigEndian(short value)
        {
            byte [] buffer = new byte[sizeof(short)];
            BinaryPrimitives.WriteInt16BigEndian(buffer, value);

            return buffer; 
        }
    }

    internal static class GreaseEchExtension
    {
        private static byte[] EncodedData = new byte[32]; 
        private static byte[] EncodedPayload = new byte[176]; 

        static GreaseEchExtension()
        {
            var random = new Random(9);
            random.NextBytes(EncodedData);
            random.NextBytes(EncodedPayload);
        }

        public static byte[] GetGreaseEncryptedClientHello()
        {
            var clientHello = new EncryptedClientHello {
                ClientHelloType = 0,
                Encoded = EncodedData,
                Payload = EncodedPayload
            };

            var encoded = clientHello.GetBytes();

            return encoded;
        }
    }

    internal struct EncryptedClientHello
    {
        public EncryptedClientHello()
        {
            Encoded = new byte[] { };
            Payload = new byte[] { };
        }

        public byte ClientHelloType { get; set; } = 0;

        public int CipherSuite { get; set; } = 0x10001; // TLS_AES_128_GCM_SHA256

        public byte ConfigId { get; set; } = 0x53;

        public byte[] Encoded { get; set; }
        
        public byte[] Payload { get; set; }

        public byte[] GetBytes()
        {
            // Marshall to byte array 

            var totalSize = 1 + 4 + 1 + 2 + 2 + Encoded.Length + Payload.Length;
            var result = new byte[totalSize];

            using (var memoryStream = new MemoryStream(result, true)) {


                TlsUtilities.WriteUint8(ClientHelloType, memoryStream);
                TlsUtilities.WriteUint32(CipherSuite, memoryStream);
                TlsUtilities.WriteUint8(ConfigId, memoryStream);
                TlsUtilities.WriteOpaque16(Encoded, memoryStream);
                TlsUtilities.WriteOpaque16(Payload, memoryStream);
            }

            return result;
        }

    }
}
