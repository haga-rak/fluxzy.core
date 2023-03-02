// // Copyright 2022 - Haga Rakotoharivelo
// 

using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;

namespace Fluxzy.Interop.Pcap.Pcapng.Structs
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly ref struct SectionHeaderBlock
    {
        public SectionHeaderBlock(int optionLength)
        {
            BlockTotalLength = 24 + optionLength + 4;
        }

        public uint BlockType { get; init; } = 0x0A0D0D0A;

        public int BlockTotalLength { get; } 

        public uint ByteOrderMagic { get; init; } = 0x1A2B3C4D;
        
        public ushort MajorVersion { get; init; } = 1;

        public ushort MinorVersion { get; init; } = 0;

        public ulong SectionLength { get; init; } = 0xFFFFFFFFFFFFFFFF;

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

    public readonly ref struct EndOfOption
    {
        public int OnWriteLength => 4;

        public int Write(Span<byte> buffer)
        {
            BinaryPrimitives.WriteInt32LittleEndian(buffer, 0);
            return 4;
        }
    }

    public readonly ref struct StringOptionBlock
    {
        public StringOptionBlock(OptionBlockCode optionCode, string optionValue)
                : this((ushort)optionCode, optionValue)
        {
            
        }
        
        public StringOptionBlock(ushort optionCode, string optionValue)
        {
            OptionCode = optionCode;
            OptionLength = (ushort) Encoding.UTF8.GetByteCount(optionValue);
            OptionValue = optionValue;
        }

        public ushort OptionCode { get; }

        public ushort OptionLength { get; }
        
        public string OptionValue { get; }

        /// <summary>
        /// This length includes padding
        /// </summary>
        public int OnWireLength => 4 + (int) OptionLength + ((4 - (int) OptionLength % 4) %4) ;
        
        
        public int Write(Span<byte> buffer)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(buffer, OptionCode);
            BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(2), OptionLength);

            // TODO control overflow here if caller provider a very long string 
            Span<byte> stringBuffer = stackalloc byte[(int) OptionLength];

            Encoding.UTF8.GetBytes(OptionValue, stringBuffer);

            stringBuffer.CopyTo(buffer.Slice(4));

            // Add padding 

            return OnWireLength;
        }
    }
}