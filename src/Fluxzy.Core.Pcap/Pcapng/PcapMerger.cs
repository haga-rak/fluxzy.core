// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Core.Pcap.Pcapng.Structs;
using System.Buffers.Binary;

namespace Fluxzy.Core.Pcap.Pcapng
{
    internal class PcapMerger
    {
        public async Task Merge(Stream outputStream, params string [] files)
        {
            var pendingPcapFiles =
                new Queue<PendingPcapFile>(files.Select(s => new PendingPcapFile(null));




            while ()
        }
    }

    internal interface IBlock
    {
        int TimeStamp { get; }

        int Length { get; }
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

        }

        public bool ReadNextPacketBlock(EnhancedPacketBlock result)
        {
            // Must advance 

            while (PcapBlocReadingHelper.GetNextBlockType())




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

            var totalBlockLength = 

        }
    }
}
