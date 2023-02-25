using System.Net;

namespace Fluxzy.Interop.Pcap.Messages;

public readonly struct SubscribeMessage
{
    public SubscribeMessage(IPAddress remoteAddress, int remotePort, int localPort, string outFileName)
    {
        RemoteAddress = remoteAddress;
        RemotePort = remotePort;
        LocalPort = localPort;
        OutFileName = outFileName;
    }
        
    public IPAddress RemoteAddress { get; }
    
    public int RemotePort { get; }
    
    public int LocalPort { get;}
    
    public string OutFileName { get; }
    
    public static SubscribeMessage FromReader(BinaryReader reader)
    {
        Span<char> charBuffer = stackalloc char[512];
        
        var remoteAddress = SerializationUtils.ReadIpAddress(reader.BaseStream);
        var remotePort = reader.ReadInt32();
        var localPort = reader.ReadInt32();
        var outFileNameLength = reader.BaseStream.ReadString(charBuffer);
        var outFileName = new string(charBuffer.Slice(0, outFileNameLength));

        return new SubscribeMessage(remoteAddress, remotePort, localPort, outFileName);
    }

    public void Write(BinaryWriter writer)
    {
        writer.BaseStream.WriteString(RemoteAddress.ToString());
        writer.Write(RemotePort);
        writer.Write(LocalPort);
        writer.BaseStream.WriteString(OutFileName);
    }

    public bool Equals(SubscribeMessage other)
    {
        return RemoteAddress.Equals(other.RemoteAddress) && RemotePort == other.RemotePort && LocalPort == other.LocalPort && OutFileName == other.OutFileName;
    }

    public override bool Equals(object? obj)
    {
        return obj is SubscribeMessage other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(RemoteAddress, RemotePort, LocalPort, OutFileName);
    }

}