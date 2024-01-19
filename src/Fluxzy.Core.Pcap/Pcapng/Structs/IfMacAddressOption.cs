// // Copyright 2022 - Haga Rakotoharivelo
// 

using System.Buffers.Binary;

namespace Fluxzy.Core.Pcap.Pcapng.Structs
{
    internal readonly struct IfMacAddressOption : IOptionBlock
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

        public static IfMacAddressOption Parse(ReadOnlySpan<byte> buffer)
        {
            return new IfMacAddressOption(buffer.Slice(4, 6).ToArray());
        }

        public static int DirectWrite(Span<byte> buffer)
        {
            return new IfMacAddressOption().Write(buffer);
        }
    }
}