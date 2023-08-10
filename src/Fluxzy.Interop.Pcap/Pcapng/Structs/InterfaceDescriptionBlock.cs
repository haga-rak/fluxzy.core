using System.Buffers.Binary;

namespace Fluxzy.Interop.Pcap.Pcapng.Structs
{
    internal readonly struct InterfaceDescriptionBlock
    {
        private readonly InterfaceDescription _interfaceDescription;
        private readonly List<IOptionBlock> _options = new(); 

        public InterfaceDescriptionBlock(InterfaceDescription interfaceDescription)
        {
            _interfaceDescription = interfaceDescription;
            BlockTotalLength = 0;

            BlockTotalLength = 16 + 4;

            _options.Add(new TsResolOption(6));

            if (!string.IsNullOrWhiteSpace(interfaceDescription.Description)) {
                _options.Add(new StringOptionBlock(OptionBlockCode.If_Description, interfaceDescription.Description));
            }
            
            if (!string.IsNullOrWhiteSpace(interfaceDescription.Name)) {
                _options.Add(new StringOptionBlock(OptionBlockCode.If_Name, interfaceDescription.Name));
            }
            
            if (interfaceDescription.MacAddress != null) {
                _options.Add(new IfMacAddressOption(interfaceDescription.MacAddress));
            }


            if (_options.Any()) {
                _options.Add(new EndOfOption());
            }

            BlockTotalLength += _options.Sum(o => o.OnWireLength); 

            LinkType = interfaceDescription.LinkType;

        }

        public uint BlockType => 0x00000001;

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
}