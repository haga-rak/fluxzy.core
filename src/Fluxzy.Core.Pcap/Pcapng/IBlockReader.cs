namespace Fluxzy.Core.Pcap.Pcapng
{
    internal interface IBlockReader
    {
        /// <summary>
        ///  Release temporary resources if any 
        /// </summary>
        void Sleep();
    }

    internal interface IBlockReader<T> : IDisposable, IBlockReader
        where T : struct
    {
        int NextTimeStamp { get; }

        bool Dequeue(out T result);
    }
}
