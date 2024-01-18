// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Core.Pcap.Pcapng.Structs;
using System.Buffers.Binary;

namespace Fluxzy.Core.Pcap.Pcapng
{
    internal class PcapMerger<T, TArgs> where TArgs : notnull
    {
        public void Merge(IBlockWriter<T> writer,
            Func<TArgs, IBlockReader<T>> blockFactory,
            params TArgs[] items)
        {
            var array = items.Select(blockFactory).ToArray();

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

    internal interface IBlockReader<out T>
    {
        int ? NextTimeStamp { get; }

        T? Dequeue();
    }

    internal class PendingPcapFile
    {
        private readonly DormantReadStream _dormantStream;

        public PendingPcapFile(DormantReadStream dormantStream)
        {
            _dormantStream = dormantStream;
        }
        
        public int? NextPacketBlockTimeStamp()
        {
            return null; 
        }

        public bool ReadNextPacketBlock(EnhancedPacketBlock result)
        {
            // Must advance 
            return false; 
        }
    }


    internal static class PcapBlocReadingHelper
    {
        public static uint GetNextBlockType(DormantReadStream stream)
        {
            var nextFourBytes = new byte[4];

            if (!stream.ReadExact(nextFourBytes))
                return 0; // No more readable block 

            return BinaryPrimitives.ReadUInt32LittleEndian(nextFourBytes);
        }

        /// <summary>
        ///  This method assume that block type is already parsed 
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static SectionHeaderBlock ReadNextBlock(DormantReadStream stream)
        {
            var sectionHeaderBlock = new SectionHeaderBlock();

            return default;

        }
    }
}
