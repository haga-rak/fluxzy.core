// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Core.Socks5
{
    internal readonly struct Socks5Request
    {
        public Socks5Request(
            byte command,
            byte addressType,
            string destinationAddress,
            int destinationPort,
            byte[] rawAddress)
        {
            Command = command;
            AddressType = addressType;
            DestinationAddress = destinationAddress;
            DestinationPort = destinationPort;
            RawAddress = rawAddress;
        }

        public byte Command { get; }

        public byte AddressType { get; }

        public string DestinationAddress { get; }

        public int DestinationPort { get; }

        public byte[] RawAddress { get; }
    }
}
