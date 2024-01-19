// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Core.Pcap.Pcapng.Structs;
using System.Buffers.Binary;

namespace Fluxzy.Core.Pcap.Pcapng
{
    internal class EnhancedBlockReader : SleepyStreamBlockReader
    {
        private readonly GenericBlockHandler _blockHandler;
        private readonly byte[] _defaultBuffer = new byte[1024 * 2]; 

        public EnhancedBlockReader(GenericBlockHandler blockHandler,
            StreamLimiter streamLimiter, Func<Stream> streamFactory)
            : base(streamLimiter, streamFactory)
        {
            _blockHandler = blockHandler;
        }

        protected override bool ReadNextBlock(SleepyStream stream, out DataBlock result)
        {
            Span<byte> buffer = _defaultBuffer.AsSpan().Slice(0,8);

            for (;;)
            {
                var read = stream.ReadExact(buffer);

                if (!read) {
                    result = default;
                    return false; 
                }

                var blockType = BinaryPrimitives.ReadUInt32LittleEndian(buffer);
                var blockTotalLength = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(4));

                var compare = SectionHeaderBlock.BlockTypeValue == blockType;

                if (blockType != EnhancedPacketBlock.BlockTypeValue)
                {
                    // DO something with other block type 

                    if (blockTotalLength > _defaultBuffer.Length) {
                        throw new InvalidOperationException(
                            $"Block length exceed default buffer length {blockTotalLength} > {_defaultBuffer.Length}");
                    }

                    if (!stream.ReadExact(_defaultBuffer.AsSpan(8, blockTotalLength - 8)))
                    {
                        result = default;
                        return false; // Unable to read
                    } 

                    if (!_blockHandler.NotifyNewBlock(blockType, _defaultBuffer.AsSpan(0, blockTotalLength))) {
                        result = default;
                        return false;  // EARLY EOF
                    }

                    continue;
                }

                // This allocation is not ideal but we can't use a
                // fixed buffer because the block length is variable

                Span<byte> data = new byte[blockTotalLength];

                var readData = stream.ReadExact(data.Slice(8));

                if (!readData) {
                    result = default;
                    return false; 
                }

                var timeStamp = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(4));

                BinaryPrimitives.WriteUInt32LittleEndian(data, blockType);
                BinaryPrimitives.WriteInt32LittleEndian(data.Slice(4), blockTotalLength);

                result = new DataBlock(timeStamp, data.ToArray());

                return true; 
            }
        }
    }
}
