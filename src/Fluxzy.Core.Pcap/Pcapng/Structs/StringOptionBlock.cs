using System.Buffers.Binary;
using System.Text;

namespace Fluxzy.Core.Pcap.Pcapng.Structs
{
    internal readonly struct StringOptionBlock : IOptionBlock
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

            Span<byte> stringBuffer = 
                OptionLength < FluxzySharedSetting.StackAllocThreshold ?
                    stackalloc byte[(int) OptionLength] : new byte[OptionLength];

            Encoding.UTF8.GetBytes(OptionValue, stringBuffer);

            stringBuffer.CopyTo(buffer.Slice(4));

            // Add padding 

            return OnWireLength;
        }

        public static StringOptionBlock Parse(ReadOnlySpan<byte> buffer)
        {
            var optionLength = BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(2));
            var valueBuffer = buffer.Slice(4, optionLength);
            string value = Encoding.UTF8.GetString(valueBuffer);

            return new StringOptionBlock(BinaryPrimitives.ReadUInt16LittleEndian(buffer), value);
        }
    }
}