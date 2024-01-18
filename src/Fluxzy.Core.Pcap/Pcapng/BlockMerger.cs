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
            if (!items.Any())
                return;

            var array = items.Select(blockFactory).ToArray();

            Array.Sort(array, BlockComparer<T>.Instance);

            while (true)
            {
                var nextTimeStamp = array[0].Dequeue(); 

                if (nextTimeStamp == null)
                    break; // No more block to read
                
                writer.Write(nextTimeStamp);

                ArrayUtilities.Reposition(array,
                    array[0], BlockComparer<T>.Instance);
            }

            foreach (var resource in array)
            {
                resource.Dispose();
            }
            
        }
    }

    internal static class ArrayUtilities
    {
        public static void Reposition<T>(T[] sortedArrayAtIndex1, T firstElement, 
            IComparer<T> comparer)
        {
            for (int i = 1; i < sortedArrayAtIndex1.Length; i++) {
                var current = sortedArrayAtIndex1[i];

                if (comparer.Compare(firstElement, current) <= 0) {
                    break;
                }

                sortedArrayAtIndex1[i - 1] = current;
                sortedArrayAtIndex1[i] = firstElement;
            }
        }
    }

    internal interface IBlockWriter<in T>
    {
        public void Write(T content); 
    }

    internal class BlockComparer<T> : IComparer<IBlockReader<T>>
    {
        public static readonly BlockComparer<T> Instance = new();

        private BlockComparer()
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
