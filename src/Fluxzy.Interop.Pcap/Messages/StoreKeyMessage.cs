// // Copyright 2022 - Haga Rakotoharivelo
// 

using System.Net;

namespace Fluxzy.Interop.Pcap.Messages
{
    public readonly struct StoreKeyMessage
    {
        public StoreKeyMessage(IPAddress remoteAddress, int remotePort, int localPort, string nssKey)
        {
            RemoteAddress = remoteAddress;
            RemotePort = remotePort;
            LocalPort = localPort;
            NssKey = nssKey;
        }

        public IPAddress RemoteAddress { get; }

        public int RemotePort { get; }

        public int LocalPort { get; }

        public string NssKey { get; }

        public static StoreKeyMessage FromReader(BinaryReader reader)
        {
            Span<char> charBuffer = stackalloc char[512];

            var remoteAddress = SerializationUtils.ReadIpAddress(reader.BaseStream);
            var remotePort = reader.ReadInt32();
            var localPort = reader.ReadInt32();
            var outFileNameLength = reader.BaseStream.ReadString(charBuffer);
            var outFileName = new string(charBuffer.Slice(0, outFileNameLength));

            return new StoreKeyMessage(remoteAddress, remotePort, localPort, outFileName);
        }

        public void Write(BinaryWriter writer)
        {
            writer.BaseStream.WriteString(RemoteAddress.ToString());
            writer.Write(RemotePort);
            writer.Write(LocalPort);
            writer.BaseStream.WriteString(NssKey);
        }

        public bool Equals(StoreKeyMessage other)
        {
            return RemoteAddress.Equals(other.RemoteAddress) && RemotePort == other.RemotePort &&
                   LocalPort == other.LocalPort && NssKey == other.NssKey;
        }

        public override bool Equals(object? obj)
        {
            return obj is StoreKeyMessage other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(RemoteAddress, RemotePort, LocalPort, NssKey);
        }
    }
}
