namespace Fluxzy.Core.Pcap.Pcapng
{
    internal interface IBlockReader : IDisposable
    {
        /// <summary>
        ///  Next timestamp
        /// </summary>
        uint NextTimeStamp { get; }

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


    internal struct DataBlock
    {
        public DataBlock(uint timeStamp, ReadOnlyMemory<byte> data)
        {
            TimeStamp = timeStamp;
            Data = data;
        }

        public uint TimeStamp { get;  }

        public ReadOnlyMemory<byte> Data { get; }
    }
}
