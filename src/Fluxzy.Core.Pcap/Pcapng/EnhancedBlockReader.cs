// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Core.Pcap.Pcapng.Structs;
using System.Buffers.Binary;

namespace Fluxzy.Core.Pcap.Pcapng
{
    internal readonly struct EnhancedBlock
    {
        public EnhancedBlock(int timeStamp, byte[] data)
        {
            TimeStamp = timeStamp;
            Data = data;
        }

        public int TimeStamp { get; }

        public byte[] Data { get; }
    }

    public interface IInt<T> where T : struct
    {
        T Value { get; }
    }

    internal class EnhancedBlockReader : SleepyStreamBlockReader
    {
        public EnhancedBlockReader(
            StreamLimiter streamLimiter, Func<Stream> streamFactory)
            : base(streamLimiter, streamFactory)
        {

        }

        protected override bool ReadNextBlock(SleepyStream stream, out DataBlock result)
        {
            // BinaryPrimitives.WriteUInt32LittleEndian(buffer, BlockType);
            // BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(4), BlockTotalLength);

            Span<byte> buffer = stackalloc byte[8];

            for (;;)
            {
                var read = stream.ReadExact(buffer);

                if (!read) {
                    result = default;
                    return false; 
                }

                var blockType = BinaryPrimitives.ReadUInt32LittleEndian(buffer);
                var blockTotalLength = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(4));

                if (blockType != EnhancedPacketBlock.BlockTypeValue)
                {
                    // DO something with other block type 
                    continue;
                }

                var data = new byte[blockTotalLength];

                var readData = stream.ReadExact(data);

                if (!readData) {
                    result = default;
                    return false; 
                }
            }
        }

        protected override uint ReadTimeStamp(ref DataBlock block)
        {
            throw new NotImplementedException();
        }
    }
}
