namespace Fluxzy.Core.Pcap.Pcapng.Merge
{
    internal interface IBlockWriter
    {
        public void Write(ref DataBlock content);
    }
}