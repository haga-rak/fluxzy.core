// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Core.Socks5
{
    internal static class Socks5Constants
    {
        public const byte Version = 0x05;
        public const byte AuthVersion = 0x01;
        public const byte Reserved = 0x00;

        // Commands (RFC 1928)
        public const byte CmdConnect = 0x01;
        public const byte CmdBind = 0x02;
        public const byte CmdUdpAssociate = 0x03;

        // Authentication methods
        public const byte AuthNoAuth = 0x00;
        public const byte AuthGssApi = 0x01;
        public const byte AuthUsernamePassword = 0x02;
        public const byte AuthNoAcceptable = 0xFF;

        // Address types
        public const byte AddrTypeIPv4 = 0x01;
        public const byte AddrTypeDomain = 0x03;
        public const byte AddrTypeIPv6 = 0x04;

        // Reply codes
        public const byte RepSucceeded = 0x00;
        public const byte RepGeneralFailure = 0x01;
        public const byte RepConnectionNotAllowed = 0x02;
        public const byte RepNetworkUnreachable = 0x03;
        public const byte RepHostUnreachable = 0x04;
        public const byte RepConnectionRefused = 0x05;
        public const byte RepTtlExpired = 0x06;
        public const byte RepCommandNotSupported = 0x07;
        public const byte RepAddressTypeNotSupported = 0x08;

        // Sizes
        public const int IPv4AddressLength = 4;
        public const int IPv6AddressLength = 16;
        public const int PortLength = 2;
    }
}
