// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Core.Pcap.Pcapng
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TArgs"></typeparam>
    internal class BlockMerger<T, TArgs> where TArgs : notnull
        where T : struct
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
                if (!array[0].Dequeue(out var block))
                    break; // No more block to read
                
                writer.Write(block);

                ArrayUtilities.Reposition(array, array[0], BlockComparer<T>.Instance);
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
            for (int i = 1; i < sortedArrayAtIndex1.Length; i++)
            {
                var current = sortedArrayAtIndex1[i];

                if (comparer.Compare(firstElement, current) <= 0) {
                    return;
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
        where T : struct
    {
        public static readonly BlockComparer<T> Instance = new();

        private BlockComparer()
        {

        }

        public int Compare(IBlockReader<T>? x, IBlockReader<T>? y)
        {
            var xTimeStamp = x!.NextTimeStamp;

            if (xTimeStamp == -1)
            {
                return 1;
            }

            var yTimeStamp = y!.NextTimeStamp; 

            if (yTimeStamp == -1) {
                return -1;
            }

            return xTimeStamp.CompareTo(yTimeStamp);
        }
    }

    internal interface IBlockReader<T> : IDisposable, IBlockReader 
      where T : struct
    {
        int NextTimeStamp { get; }

        bool Dequeue(out T result);
    }

    internal interface IBlockReader
    {
        /// <summary>
        ///  Release temporary resources if any 
        /// </summary>
        void Sleep();
    }

}
