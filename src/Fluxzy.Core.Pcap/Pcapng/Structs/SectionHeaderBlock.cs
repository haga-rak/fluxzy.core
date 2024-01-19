// // Copyright 2022 - Haga Rakotoharivelo
// 

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;

namespace Fluxzy.Core.Pcap.Pcapng.Structs
{
    internal readonly ref struct SectionHeaderBlock
    {
        public const int BlockTypeValue = 0x0A0D0D0A;

        public SectionHeaderBlock(int optionLength)
        {
            BlockTotalLength = 24 + optionLength + 4;
        }

        public uint BlockType => BlockTypeValue;

        public int BlockTotalLength { get; }

        public uint ByteOrderMagic => 0x1A2B3C4D;

        public ushort MajorVersion => 1;

        public ushort MinorVersion => 0;

        public ulong SectionLength => 0xFFFFFFFFFFFFFFFF;

        public int OnWireLength => BlockTotalLength;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int WriteTrailer(Span<byte> buffer)
        {
            BinaryPrimitives.WriteInt32LittleEndian(buffer, BlockTotalLength);

            return 4;
        }

        public static Dictionary<OptionBlockCode, string> Parse(ReadOnlySpan<byte> buffer)
        {
            var optionBlocks = buffer.Slice(24);

            var result = new Dictionary<OptionBlockCode, string>();

            while (optionBlocks.Length >= 4) {
                var optionCode = (OptionBlockCode) BinaryPrimitives.ReadUInt16LittleEndian(optionBlocks);

                if (optionCode == OptionBlockCode.Opt_EndOfOpt)
                    break;  // End of options

                var optionLength = BinaryPrimitives.ReadUInt16LittleEndian(optionBlocks.Slice(2));

                if (optionBlocks.Length < 4 + optionLength)
                    throw new InvalidOperationException("Option block bad size");  // No more option

                var str = Encoding.UTF8.GetString(optionBlocks.Slice(4, optionLength));
                result[optionCode] = str;

                var paddingLength = optionLength % 4; 

                optionBlocks = optionBlocks.Slice(4 + optionLength + paddingLength);
            }

            return result;
        }
    }
}
