namespace Fluxzy.Interop.Pcap.Reading
{
    public ref struct TcpPacketInfo
    {
        public int SourcePort { get; set; }

        public int DestinationPort { get; set; }
    }
}