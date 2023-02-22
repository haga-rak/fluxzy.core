using System;
using System.IO;
using System.Net;

namespace Fluxzy.Capturing.Messages;

internal static class SerializationUtils
{
    public static IPAddress ReadIpAddress(BinaryReader reader, Span<char> charBuffer)
    {
        var addressLength = reader.Read(charBuffer);
        var remoteAddress = IPAddress.Parse(charBuffer.Slice(0, addressLength));
        return remoteAddress;
    }
}