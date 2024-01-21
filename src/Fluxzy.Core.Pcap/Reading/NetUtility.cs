namespace Fluxzy.Core.Pcap.Reading
{
    internal static class NetUtility
    {
        public static long MacToLong(ReadOnlySpan<byte> macAddress)
        {
            Span<byte> destination = stackalloc byte[8];

            macAddress.CopyTo(destination.Slice(2));

            // We do not care about endianess here as we are only interested
            // in a mapping between physical address and a long value in a capture session

            return BitConverter.ToInt64(destination);
        }
    }
}
