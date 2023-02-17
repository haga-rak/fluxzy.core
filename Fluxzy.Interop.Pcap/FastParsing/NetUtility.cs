namespace Fluxzy.Interop.Pcap.FastParsing
{
    internal static class NetUtility
    {
        public static long MacToLong(ReadOnlySpan<byte> macAddress)
        {
            if (macAddress.Length != 6)
                throw new ArgumentException("Must be 6 byte length", nameof(macAddress));

            Span<byte> destination = stackalloc byte[8]; 

            macAddress.CopyTo(destination.Slice(2));

            // We do not care about endianess here as we are only interested
            // in a mapping between physical address and a long value
            
            return BitConverter.ToInt64(destination);
        }
    }
}