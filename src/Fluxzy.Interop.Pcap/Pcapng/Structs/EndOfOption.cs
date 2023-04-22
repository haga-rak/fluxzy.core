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
}