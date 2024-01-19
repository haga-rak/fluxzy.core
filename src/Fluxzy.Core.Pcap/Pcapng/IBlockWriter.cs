namespace Fluxzy.Core.Pcap.Pcapng
{
    internal interface IBlockWriter<T> 
        where T : struct
    {
        public void Write(ref T content); 
    }
}