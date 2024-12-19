// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using Org.BouncyCastle.Tls;

namespace Fluxzy.Clients.Ssl.BouncyCastle
{
    public static class ClientExtensionHelper
    {
        private static readonly byte[] Http2ApplicationProtocol = new byte[5] { 0, 0x3, 0x02, 0x68, 0x32 };
        private static readonly byte[] EmptyOctet = new byte[1]; 

        public static IDictionary<int, byte[]> AdjustClientExtensions(IDictionary<int, byte[]> current,
            Ja3FingerPrint fingerPrint, string targetHost)
        {
            var result = current;
            var clientExtensionTypes = fingerPrint.ClientExtensions; 

            // Remove 
            foreach (var type in clientExtensionTypes.Where(t => !current.ContainsKey(t)))
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

                var extensionData = GetDefaultClientValueExtension(type, targetHost);

                current[type] = extensionData ??
                                throw new InvalidOperationException($"Unhandled extension {type}");
            }

            return result;
        }

        internal static byte[]? GetDefaultClientValueExtension(
            int type,
            string targetHost)
        {
            if (type == 0)
                return ServerNameUtilities.CreateFromHost(targetHost);

            if (type == ExtensionType.renegotiation_info)
                return EmptyOctet;

            if (type == ExtensionType.signed_certificate_timestamp)
                return Array.Empty<byte>();

            if (type == ExtensionType.compress_certificate)
                return TlsExtensionsUtilities.CreateCompressCertificateExtension(new[] { 2 });

            if (type == ExtensionType.session_ticket)
                return Array.Empty<byte>();

            if (type == ExtensionType.psk_key_exchange_modes)
                return TlsExtensionsUtilities.CreatePskKeyExchangeModesExtension(new short[] { 1 });

            if (type == 0x4469) // APPLICATION PROTOCOLS 17513 --> https://chromestatus.com/feature/5149147365900288
                return Http2ApplicationProtocol;

            return null; 
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
}
