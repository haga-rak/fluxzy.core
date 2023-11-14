namespace Fluxzy.Core.Pcap.Reading
{
    internal ref struct TcpPacketInfo
    {
        public int SourcePort { get; set; }

        public int DestinationPort { get; set; }
    }
}