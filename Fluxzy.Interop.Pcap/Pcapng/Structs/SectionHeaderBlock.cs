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
    

    public interface IOptionBlock
    {
        int OnWireLength { get;  }

        int Write(Span<byte> buffer); 
    }
    
    public readonly struct EndOfOption : IOptionBlock
    {
        public int OnWireLength => 4;

        public int Write(Span<byte> buffer)
        {
            BinaryPrimitives.WriteInt32LittleEndian(buffer, 0);
            return 4;
        }

        public static int DirectWrite(Span<byte> buffer)
        {
            return new EndOfOption().Write(buffer);
        }
    }

    public readonly struct StringOptionBlock : IOptionBlock
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

        public static int Write(Span<byte> buffer, OptionBlockCode optionCode, string optionValue)
        {
            return new StringOptionBlock(optionCode, optionValue).Write(buffer);
        }

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

    public readonly struct InterfaceDescriptionBlock
    {
        private readonly InterfaceDescription _interfaceDescription;
        private readonly List<IOptionBlock> _options = new(); 

        public InterfaceDescriptionBlock(InterfaceDescription interfaceDescription)
        {
            _interfaceDescription = interfaceDescription;
            BlockTotalLength = 0;

            BlockTotalLength = 16 + 4;

            if (!string.IsNullOrWhiteSpace(interfaceDescription.Description)) {
                _options.Add(new StringOptionBlock(OptionBlockCode.If_Description, interfaceDescription.Description));
            }
            
            if (!string.IsNullOrWhiteSpace(interfaceDescription.Name)) {
                _options.Add(new StringOptionBlock(OptionBlockCode.If_Name, interfaceDescription.Name));
            }

            if (_options.Any()) {
                _options.Add(new EndOfOption());
            }

            BlockTotalLength += _options.Sum(o => o.OnWireLength); 

            LinkType = interfaceDescription.LinkType;

        }

        public uint BlockType { get; init; } = 0x00000001;

        public int BlockTotalLength { get; }

        public ushort LinkType { get;  }

        public ushort Reserved => 0;

        public int SnapLen => 0;

        public int Write(Span<byte> buffer)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(buffer, BlockType);
            BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(4), BlockTotalLength);
            BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(8), LinkType);
            BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(10), Reserved);
            BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(12), SnapLen);

            var offset = 16;

            foreach (var option in _options)
            {
                offset += option.Write(buffer.Slice(offset));
            }

            BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(offset), BlockTotalLength);

            return BlockTotalLength; 
        }
    }


    public class InterfaceDescription
    {
        public InterfaceDescription(ushort linkType, int interfaceId)
        {
            LinkType = linkType;
            InterfaceId = interfaceId;
        }
        public int InterfaceId { get; }
        
        public string? Name { get; set; }

        public string? Description { get; set; }

        public ushort LinkType { get; }
    }
    

    public readonly ref struct EnhancedPacketBlock
    {
        private readonly string? _comment;

        public EnhancedPacketBlock(
            int interfaceId, 
            uint timestampHigh, uint timestampLow, 
            int capturedLength, int originalLength, 
            string?  comment = null)
        {
            _comment = comment;
            InterfaceId = interfaceId;
            TimestampHigh = timestampHigh;
            TimestampLow = timestampLow;
            CapturedLength = capturedLength;
            OriginalLength = originalLength;

            var packetPaddedLength = capturedLength + ((4 - (int)capturedLength % 4) % 4);

            BlockTotalLength = 32 + packetPaddedLength;

            if (!string.IsNullOrWhiteSpace(comment)) {
                BlockTotalLength += OptionHelper.GetOnWireLength(comment); 
            }
        }

        public uint BlockType { get; init; } = 0x00000006;

        public int BlockTotalLength { get; }

        public int InterfaceId { get; }

        public uint TimestampHigh { get; }

        public uint TimestampLow { get; }

        public int CapturedLength { get; }

        public int OriginalLength { get; }


        public int Write(Span<byte> buffer, ReadOnlySpan<byte> payload)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(buffer, BlockType);
            BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(4), BlockTotalLength);
            BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(8), InterfaceId);
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(12), TimestampHigh);
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(16), TimestampLow);
            BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(20), CapturedLength);
            BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(24), OriginalLength);

            payload.Slice(0, CapturedLength).CopyTo(buffer.Slice(28));

            var offset = 28 + CapturedLength;

            if (!string.IsNullOrWhiteSpace(_comment))
            {
                offset += StringOptionBlock.Write(buffer.Slice(offset), OptionBlockCode.Opt_Comment, _comment); 
                offset += EndOfOption.DirectWrite(buffer.Slice(offset)); 
            }

            BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(offset), BlockTotalLength);

            return offset + 4; // Should be block total length 
        }
    }

    
}