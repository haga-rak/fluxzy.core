namespace Fluxzy.Core.Pcap.Pcapng
{
    internal interface IBlockWriter
    {
        public void Write(ref DataBlock content); 
    }
}