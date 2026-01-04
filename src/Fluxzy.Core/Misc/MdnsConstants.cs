// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net;

namespace Fluxzy.Misc
{
    /// <summary>
    /// Constants for mDNS/DNS-SD protocol implementation.
    /// </summary>
    internal static class MdnsConstants
    {
        /// <summary>
        /// IPv4 multicast address for mDNS (RFC 6762).
        /// </summary>
        public static readonly IPAddress MulticastAddress = IPAddress.Parse("224.0.0.251");

        /// <summary>
        /// Standard mDNS port.
        /// </summary>
        public const int Port = 5353;

        /// <summary>
        /// Fluxzy proxy service type for DNS-SD.
        /// </summary>
        public const string ServiceType = "_fluxzyproxy._tcp.local";

        /// <summary>
        /// Local domain suffix.
        /// </summary>
        public const string Domain = "local";

        /// <summary>
        /// Default TTL for mDNS records (75 minutes in seconds).
        /// </summary>
        public const int DefaultTtl = 4500;

        /// <summary>
        /// DNS A record type (IPv4 address).
        /// </summary>
        public const ushort TypeA = 1;

        /// <summary>
        /// DNS PTR record type (pointer).
        /// </summary>
        public const ushort TypePTR = 12;

        /// <summary>
        /// DNS TXT record type (text).
        /// </summary>
        public const ushort TypeTXT = 16;

        /// <summary>
        /// DNS SRV record type (service).
        /// </summary>
        public const ushort TypeSRV = 33;

        /// <summary>
        /// DNS class IN (Internet).
        /// </summary>
        public const ushort ClassIN = 1;

        /// <summary>
        /// DNS class IN with cache flush flag set.
        /// </summary>
        public const ushort ClassINFlush = 0x8001;

        /// <summary>
        /// Maximum size of a single TXT record string.
        /// </summary>
        public const int MaxTxtStringLength = 255;

        /// <summary>
        /// mDNS response flags (QR=1, AA=1).
        /// </summary>
        public const ushort ResponseFlags = 0x8400;

        /// <summary>
        /// DNS ANY query type (used in mDNS service discovery).
        /// </summary>
        public const ushort TypeANY = 255;
    }
}
