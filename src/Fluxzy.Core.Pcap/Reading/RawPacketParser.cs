using System.Buffers.Binary;
using System.Net;

namespace Fluxzy.Core.Pcap.Reading
{
    internal static class RawPacketParser
    {
        public static bool TryParseEthernet(
            ref EthernetPacketInfo info,
            ReadOnlySpan<byte> data, out int length)
        {
            length = 0;

            if (data.Length < 14)
                return false;

            info.DestinationMac = NetUtility.MacToLong(data.Slice(0, 6));
            info.SourceMac = NetUtility.MacToLong(data.Slice(6, 6));
            info.EtherType = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(12, 2));

            length = 14;

            return true;
        }

        public static bool TryParseIp(ref IpPacketInfo packetInfo, ReadOnlySpan<byte> data)
        {
            if (data.Length < 20)
                return false;

            packetInfo.Version = data[0] >> 4;

            if (packetInfo.Version != 4 && packetInfo.Version != 6)
                return false;


            if (packetInfo.Version == 4) {
                packetInfo.HeaderLength = (short) ((data[0] & 0xF) * 4);

                packetInfo.IsTcp = data[9] == 6;

                if (!packetInfo.IsTcp)
                    return false;

                packetInfo.SourceIp = new IPAddress(data.Slice(12, 4));
                packetInfo.DestinationIp = new IPAddress(data.Slice(16, 4));

                packetInfo.PayloadLength = (short) (BinaryPrimitives.ReadInt16BigEndian(data.Slice(2, 2))
                                                    - packetInfo.HeaderLength);
            }
            else if (packetInfo.Version == 6) {
                if (data.Length < 40)
                    return false;

                packetInfo.HeaderLength = 40; // fixed 40 bytes with IPv6

                packetInfo.IsTcp = data[6] == 6;

                if (!packetInfo.IsTcp)
                    return false;

                packetInfo.SourceIp = new IPAddress(data.Slice(8, 16));
                packetInfo.DestinationIp = new IPAddress(data.Slice(24, 16));

                packetInfo.PayloadLength = (short) (BinaryPrimitives.ReadInt16BigEndian(data.Slice(4, 2))
                                                    - packetInfo.HeaderLength);
            }

            return true;
        }

        public static bool TryParseTcp(ref TcpPacketInfo packetInfo, ReadOnlySpan<byte> data)
        {
            if (data.Length < 20)
                return false;

            packetInfo.SourcePort = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(0, 2));
            packetInfo.DestinationPort = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(2, 2));

            return true;
        }
    }
}
