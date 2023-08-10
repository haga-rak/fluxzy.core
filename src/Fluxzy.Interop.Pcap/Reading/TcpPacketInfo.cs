namespace Fluxzy.Interop.Pcap.Reading
{
    internal ref struct TcpPacketInfo
    {
        public int SourcePort { get; set; }

        public int DestinationPort { get; set; }
    }
}