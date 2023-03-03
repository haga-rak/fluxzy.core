namespace Fluxzy.Interop.Pcap.Pcapng.Structs
{
    public interface IOptionBlock
    {
        int OnWireLength { get;  }

        int Write(Span<byte> buffer); 
    }
}