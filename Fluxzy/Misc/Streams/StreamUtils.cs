using System;
using System.IO;

namespace Fluxzy.Misc.Streams
{
    internal static class StreamUtils
    {
        public static Stream AsStream(this byte[] buffer)
        {
            return new MemoryStream(buffer);
        }

        public static Stream EmptyStream => new MemoryStream(Array.Empty<byte>());
    }
}