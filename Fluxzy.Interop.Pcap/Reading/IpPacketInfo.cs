using System.Net;

namespace Fluxzy.Interop.Pcap.Reading
{
    public ref struct IpPacketInfo
    {
        public int Version { get; set; }

        public IPAddress SourceIp { get; set; }

        public IPAddress DestinationIp { get; set; }

        public short HeaderLength { get; set; }

        public short PayloadLength { get; set; }

        public bool IsTcp { get; set; }
    }
}