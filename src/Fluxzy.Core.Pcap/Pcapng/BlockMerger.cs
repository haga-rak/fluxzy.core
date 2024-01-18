// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Core.Pcap.Pcapng
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TArgs"></typeparam>
    internal class BlockMerger<T, TArgs> where TArgs : notnull
    {
        public void Merge(IBlockWriter<T> writer,
            Func<TArgs, IBlockReader<T>> blockFactory,
            params TArgs[] items)
        {
            var array = items.Select(b => {
                // new stream open here 
                var result = blockFactory(b);
                
                return result;
            }).ToArray();

            if (array.Any())
            {
                while (true)
                {
                    Array.Sort(array, PendingPcapComparer<T>.Instance);

                    var nextTimeStamp = array[0].Dequeue(); 

                    if (nextTimeStamp == null)
                        break; // No more block to read
                    
                    writer.Write(nextTimeStamp);
                }
            }

            foreach (var resource in array) {
                resource.Dispose();
            }
        }
    }

    internal interface IBlockWriter<in T>
    {
        public void Write(T content); 
    }

    internal class PendingPcapComparer<T> : IComparer<IBlockReader<T>>
    {
        public static readonly PendingPcapComparer<T> Instance = new();

        private PendingPcapComparer()
        {

        }

        public int Compare(IBlockReader<T>? x, IBlockReader<T>? y)
        {
            var xTimeStamp = x!.NextTimeStamp;
            var yTimeStamp = y!.NextTimeStamp; 

            if (xTimeStamp == null) {
                return 1;
            }

            if (yTimeStamp == null) {
                return -1;
            }

            return xTimeStamp.Value.CompareTo(yTimeStamp.Value);
        }
    }

    internal interface IBlockReader<out T> : IDisposable, IBlockReader
    {
        int ? NextTimeStamp { get; }

        T? Dequeue();
    }

    internal interface IBlockReader
    {
        /// <summary>
        ///  Release temporary resources if any 
        /// </summary>
        void Sleep();
    }

}
