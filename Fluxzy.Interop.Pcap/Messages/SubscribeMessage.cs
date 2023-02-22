using System;
using System.IO;
using System.Net;

namespace Fluxzy.Capturing.Messages;

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
        
        var remoteAddress = SerializationUtils.ReadIpAddress(reader, charBuffer);
        var remotePort = reader.ReadInt32();
        var localPort = reader.ReadInt32();
        var outFileName = reader.ReadString(); 
        
        return new SubscribeMessage(remoteAddress, remotePort, localPort, outFileName);
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write(RemoteAddress.ToString());
        writer.Write(RemotePort);
        writer.Write(LocalPort);
        writer.Write(OutFileName);
    }
}