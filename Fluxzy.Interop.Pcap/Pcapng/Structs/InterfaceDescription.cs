namespace Fluxzy.Interop.Pcap.Pcapng.Structs
{
    public class InterfaceDescription
    {
        public InterfaceDescription(ushort linkType, int interfaceId)
        {
            LinkType = linkType;
            InterfaceId = interfaceId;
        }
        public int InterfaceId { get; }
        
        public string? Name { get; set; }

        public string? Description { get; set; }

        public byte[]? MacAddress { get; set; }

        public ushort LinkType { get; }
    }
}