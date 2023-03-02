using System.Buffers.Binary;

namespace Fluxzy.Interop.Pcap.Pcapng.Structs
{
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

    public readonly struct TsResolOption : IOptionBlock
    {
        public TsResolOption(byte resolution)
        {
            Resolution = resolution;
            OptionCode = (ushort)OptionBlockCode.If_TsResol;
            OptionLength = 1; 
        }
        
        public ushort OptionCode { get; }

        public ushort OptionLength { get; }
        
        public byte Resolution { get;  }

        public int OnWireLength => 8;

        public int Write(Span<byte> buffer)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(buffer, OptionCode);
            BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(2), OptionLength);

            buffer[4] = Resolution;
            
            return 8;
        }

        public static int DirectWrite(Span<byte> buffer)
        {
            return new TsResolOption().Write(buffer);
        }
    }


    public readonly struct IfMacAddressOption : IOptionBlock
    {
        public IfMacAddressOption(byte[] macAddress)
        {
            OptionCode = (ushort)OptionBlockCode.If_MacAddr;
            OptionLength = 6;
            MacAddress = macAddress;
        }

        public ushort OptionCode { get; }

        public ushort OptionLength { get; }

        public byte[] MacAddress { get; }

        public int OnWireLength => 12;

        public int Write(Span<byte> buffer)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(buffer, OptionCode);
            BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(2), OptionLength);

            MacAddress.CopyTo(buffer.Slice(4));

            return OnWireLength;
        }

        public static int DirectWrite(Span<byte> buffer)
        {
            return new IfMacAddressOption().Write(buffer);
        }
    }
}