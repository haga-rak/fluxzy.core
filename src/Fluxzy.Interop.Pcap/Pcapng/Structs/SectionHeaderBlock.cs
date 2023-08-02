// // Copyright 2022 - Haga Rakotoharivelo
// 

using System.Buffers.Binary;

namespace Fluxzy.Interop.Pcap.Pcapng.Structs
{
    public readonly ref struct SectionHeaderBlock
    {
        public SectionHeaderBlock(int optionLength)
        {
            BlockTotalLength = 24 + optionLength + 4;
        }

        public uint BlockType =>  0x0A0D0D0A;

        public int BlockTotalLength { get; }

        public uint ByteOrderMagic => 0x1A2B3C4D;

        public ushort MajorVersion => 1;

        public ushort MinorVersion => 0;

        public ulong SectionLength => 0xFFFFFFFFFFFFFFFF;

        public int OnWireLength => BlockTotalLength;

        public int WriteHeader(Span<byte> buffer)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(buffer, BlockType);
            BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(4), BlockTotalLength);
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(8), ByteOrderMagic);
            BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(12), MajorVersion);
            BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(14), MinorVersion);
            BinaryPrimitives.WriteUInt64LittleEndian(buffer.Slice(16), SectionLength);

            return 24;
        }

        public int WriteTrailer(Span<byte> buffer)
        {
            BinaryPrimitives.WriteInt32LittleEndian(buffer, BlockTotalLength);

            return 4;
        }
    }
}
