using System;
using System.Buffers.Binary;
using System.IO;
using System.Net;
using System.Text;
using Fluxzy.Misc.Streams;

namespace Fluxzy.Capturing.Messages;

internal static class SerializationUtils
{
    public static IPAddress ReadIpAddress(Stream stream)
    {
        Span<char> charBuffer = stackalloc char[64];

        int length = stream.ReadString(charBuffer);
        
        var remoteAddress = IPAddress.Parse(charBuffer.Slice(0, length));
        return remoteAddress;
    }

    public static void WriteString(this Stream stream, string str)
    {
        Span<byte> buffer = stackalloc byte[str.Length * 2];

        var length = Encoding.UTF8.GetBytes(str, buffer.Slice(4));
        
        BinaryPrimitives.WriteInt32BigEndian(buffer, length);
        
        stream.Write(buffer.Slice(0, 4 + length));
    }

    public static int ReadString(this Stream stream, Span<char> charBuffer)
    {
        Span<byte> buffer = stackalloc byte[1024];

        if (!stream.ReadExact(buffer.Slice(0, 4)))
            throw new InvalidOperationException("Connection close");

        var stringLength = BinaryPrimitives.ReadInt32BigEndian(buffer); 

        if (!stream.ReadExact(buffer.Slice(0, stringLength)))
            throw new InvalidOperationException("Connection close");

        return Encoding.UTF8.GetChars(buffer.Slice(0, stringLength), charBuffer);
    }
}