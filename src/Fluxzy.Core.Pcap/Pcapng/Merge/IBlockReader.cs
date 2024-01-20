namespace Fluxzy.Core.Pcap.Pcapng.Merge
{
    internal interface IBlockReader : IDisposable
    {
        /// <summary>
        ///  Next timestamp
        /// </summary>
        long NextTimeStamp { get; }

        /// <summary>
        ///  
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        bool Dequeue(out DataBlock result);

        /// <summary>
        ///  Release temporary resources if any 
        /// </summary>
        void Sleep();
    }
}
