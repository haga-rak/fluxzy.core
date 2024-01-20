// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Core.Pcap.Pcapng.Structs;
using System.Buffers.Binary;

namespace Fluxzy.Core.Pcap.Pcapng.Merge
{
    internal class EnhancedBlockReader : SleepyStreamBlockReader
    {
        private readonly PcapBlockWriter _blockWriter;
        private readonly byte[] _defaultBuffer = new byte[1024 * 4];

        public EnhancedBlockReader(PcapBlockWriter blockWriter,
            StreamLimiter streamLimiter, Func<Stream> streamFactory)
            : base(streamLimiter, streamFactory)
        {
            _blockWriter = blockWriter;
        }

        protected override bool ReadNextBlock(SleepyStream stream, out DataBlock result)
        {
            Span<byte> buffer = _defaultBuffer.AsSpan().Slice(0, 8);

            for (; ; )
            {
                var read = stream.ReadExact(buffer);

                if (!read)
                {
                    result = default;
                    return false;
                }

                var blockType = BinaryPrimitives.ReadUInt32LittleEndian(buffer);
                var blockTotalLength = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(4));

                if (blockType != EnhancedPacketBlock.BlockTypeValue)
                {
                    // DO something with other block type 

                    if (blockTotalLength > _defaultBuffer.Length)
                    {
                        throw new InvalidOperationException(
                            $"Block length exceed default buffer length {blockTotalLength} > {_defaultBuffer.Length}");
                    }

                    if (!stream.ReadExact(_defaultBuffer.AsSpan(8, blockTotalLength - 8)))
                    {
                        result = default;
                        return false; // Unable to read
                    }

                    if (!_blockWriter.NotifyNewBlock(blockType, _defaultBuffer.AsSpan(0, blockTotalLength)))
                    {
                        result = default;
                        return false;  // EARLY EOF
                    }

                    continue;
                }

                // This allocation is not ideal but we can't use a
                // fixed buffer because the block length is variable
                // Reading EnhancedPacket block

                if (blockTotalLength > _defaultBuffer.Length)
                {
                    throw new InvalidOperationException($"Block exceeds default buffer " +
                                                        $"{blockTotalLength} > {_defaultBuffer.Length}");

                }

                byte[] data = _defaultBuffer;

                var readData = stream.ReadExact(data.AsSpan(8, blockTotalLength - 8));

                if (!readData)
                {
                    result = default;
                    return false;
                }

                var timeStampHigh = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(12));
                var timeStampLow = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(16));

                long fullTimeStamp = timeStampLow;

                fullTimeStamp = (long)timeStampHigh << 32 | fullTimeStamp;

                // Restore value OK
                BinaryPrimitives.WriteUInt32LittleEndian(data, blockType);
                BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(4), blockTotalLength);

                result = new DataBlock(fullTimeStamp, data.AsMemory(0, blockTotalLength));

                return true;
            }
        }
    }
}
