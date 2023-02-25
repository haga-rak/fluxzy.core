using System.Net;

namespace Fluxzy.Interop.Pcap.Messages;

public readonly struct IncludeMessage
{
    public IncludeMessage(IPAddress remoteAddress, int remotePort)
    {
        RemoteAddress = remoteAddress;
        RemotePort = remotePort; 
    }

    public int RemotePort { get; }

    public IPAddress RemoteAddress { get; }
        
    public static IncludeMessage FromReader(BinaryReader reader)
    {
        Span<char> charBuffer = stackalloc char[512];
        
        var remoteAddress = SerializationUtils.ReadIpAddress(reader.BaseStream);
        var remotePort = reader.ReadInt32();
        
        return new IncludeMessage(remoteAddress, remotePort);
    }

    public void Write(BinaryWriter writer)
    {
        writer.BaseStream.WriteString(RemoteAddress.ToString());
        writer.Write(RemotePort);
    }

    public bool Equals(IncludeMessage other)
    {
        return RemotePort == other.RemotePort && RemoteAddress.Equals(other.RemoteAddress);
    }

    public override bool Equals(object? obj)
    {
        return obj is IncludeMessage other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(RemotePort, RemoteAddress);
    }

}