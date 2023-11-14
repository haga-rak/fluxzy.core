using System.Net;

namespace Fluxzy.Core.Pcap.Reading
{
    internal ref struct IpPacketInfo
    {
        public int Version { get; set; }

        public IPAddress SourceIp { get; set; }

        public IPAddress DestinationIp { get; set; }

        public short HeaderLength { get; set; }

        public short PayloadLength { get; set; }

        public bool IsTcp { get; set; }
    }
}