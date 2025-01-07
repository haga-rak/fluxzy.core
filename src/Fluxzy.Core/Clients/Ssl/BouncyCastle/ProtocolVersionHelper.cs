// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using Org.BouncyCastle.Tls;

#pragma warning disable SYSLIB0039

namespace Fluxzy.Clients.Ssl.BouncyCastle
{
    internal static class ProtocolVersionHelper
    {
        private static readonly ProtocolVersion[] SupportedVersions = new ProtocolVersion[]
        {
            ProtocolVersion.TLSv10,
            ProtocolVersion.TLSv11,
            ProtocolVersion.TLSv12,
            ProtocolVersion.TLSv13
        };

        private static readonly Dictionary<ProtocolKey, ProtocolVersion[]?> CachedValues = new Dictionary<ProtocolKey, ProtocolVersion[]?>();

        static ProtocolVersionHelper()
        {
            int?[] plainVersions = new int?[] {
                ProtocolVersion.TLSv10.FullVersion,
                ProtocolVersion.TLSv11.FullVersion,
                ProtocolVersion.TLSv12.FullVersion,
                ProtocolVersion.TLSv13.FullVersion,
                null,
            };

            bool[] greaseModes = new[] { false, true };
            var netProtocols = (SslProtocols[]) Enum.GetValues(typeof(SslProtocols));

            foreach (var netProtocol in netProtocols)
            {
                foreach (var greaseMode in greaseModes)
                {
                    foreach (var plainVersion in plainVersions)
                    {
                        var protocolKey = new ProtocolKey(plainVersion, greaseMode, netProtocol);
                        CachedValues[protocolKey] = InternalGetProtocolVersions(plainVersion, greaseMode, netProtocol);
                    }
                }
            }
        }
        
        public static ProtocolVersion GetFromRawValue(int protocolVersion)
        {
            var result =  SupportedVersions.FirstOrDefault(v => (int)v.FullVersion == protocolVersion);

            if (result == null)
            {
                throw new ArgumentException($"Invalid protocol version {protocolVersion}");
            }

            return result;
        }

        public static ProtocolVersion[]? GetProtocolVersions(
            int? plainProtocolVersion, bool greaseMode, SslProtocols protocols)
        {
            var protocolKey = new ProtocolKey(plainProtocolVersion, greaseMode, protocols);

            if (CachedValues.TryGetValue(protocolKey, out var result))
            {
                return result;
            }

            return InternalGetProtocolVersions(plainProtocolVersion, greaseMode, protocols);
        }

        private static ProtocolVersion[]? InternalGetProtocolVersions(int? plainProtocolVersion, bool greaseMode, SslProtocols protocols)
        {
            if (plainProtocolVersion != null)
            {
                var version = GetFromRawValue(plainProtocolVersion.Value);

                if (version.IsEarlierVersionOf(ProtocolVersion.TLSv12))
                    return version.Only();

                if (greaseMode)
                {
                    // those allocation are shity
                    return new[] { ProtocolVersion.Grease }.Concat(version.DownTo(ProtocolVersion.TLSv12))
                                                           .ToArray();
                }

                return version.DownTo(ProtocolVersion.TLSv12);
            }

            if (SslProtocols.None == protocols)
            {
                return null;
            }

            var listProtocolVersion = new List<ProtocolVersion>();

            if (protocols.HasFlag(SslProtocols.Tls))
            {
                listProtocolVersion.Add(ProtocolVersion.TLSv10);
            }

            if (protocols.HasFlag(SslProtocols.Tls11))
            {
                listProtocolVersion.Add(ProtocolVersion.TLSv11);
            }

            if (protocols.HasFlag(SslProtocols.Tls12))
            {
                listProtocolVersion.Add(ProtocolVersion.TLSv12);
            }

#if NET6_0_OR_GREATER
            if (protocols.HasFlag(SslProtocols.Tls13))
            {
                listProtocolVersion.Add(ProtocolVersion.TLSv13);
            }
#endif

            return listProtocolVersion.ToArray();
        }


    }

    internal readonly struct ProtocolKey
    {
        public ProtocolKey(int? plainProtocolVersion, bool greaseMode, SslProtocols sslProtocols)
        {
            PlainProtocolVersion = plainProtocolVersion;
            GreaseMode = greaseMode;
            SslProtocols = sslProtocols;
        }

        public int ? PlainProtocolVersion { get; }

        public bool GreaseMode { get;  }

        public SslProtocols SslProtocols { get; }

        public bool Equals(ProtocolKey other)
        {
            return PlainProtocolVersion == other.PlainProtocolVersion && GreaseMode == other.GreaseMode && SslProtocols == other.SslProtocols;
        }

        public override bool Equals(object? obj)
        {
            return obj is ProtocolKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PlainProtocolVersion, GreaseMode, (int)SslProtocols);
        }

    }

}
