namespace Fluxzy.Core.Pcap.Reading
{
    internal ref struct EthernetPacketInfo
    {
        public long DestinationMac { get; set; }

        public long SourceMac { get; set; }

        public ushort EtherType { get; set; }

        public bool IsIPv4 => EtherType == 0x0800;

        public bool IsIPv6 => EtherType == 0x86DD;
    }
}
