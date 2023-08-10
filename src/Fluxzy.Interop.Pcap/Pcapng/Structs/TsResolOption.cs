// Copyright 2023 - Haga Rakotoharivelo

using System.Buffers.Binary;

namespace Fluxzy.Interop.Pcap.Pcapng.Structs
{
    internal readonly struct TsResolOption : IOptionBlock
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
}
