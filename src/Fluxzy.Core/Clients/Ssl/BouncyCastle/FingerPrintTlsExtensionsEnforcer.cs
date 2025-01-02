// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Org.BouncyCastle.Tls;

namespace Fluxzy.Clients.Ssl.BouncyCastle
{
    public class FingerPrintTlsExtensionsEnforcer
    {
        internal static readonly HashSet<int> UnsupportedClientExtensions
            = new HashSet<int>() { 34 };

        public IDictionary<int, byte[]> PrepareExtensions(IDictionary<int, byte[]> current,
            Ja3FingerPrint fingerPrint, string targetHost, ProtocolVersion[] protocolVersions)
        {
            var clientExtensionTypes = fingerPrint.ClientExtensions;

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

    public record struct AppendableExtension
    {
        public AppendableExtension(int type, byte[] data)
        {
            Type = type;
            Data = data;
        }

        public int Type { get; }

        public byte[] Data { get; }
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
}
