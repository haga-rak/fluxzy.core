using System.Buffers.Binary;

namespace Fluxzy.Core.Pcap.Pcapng.Structs
{
    internal readonly struct InterfaceDescriptionBlock
    {
        public const uint BlockTypeValue = 0x00000001;

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

        public uint BlockType => BlockTypeValue;

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

        public static InterfaceDescription Parse(ReadOnlySpan<byte> buffer)
        {
            // We ignore the first 8 bytes, as they are the block type and block total length

            var linkType = BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(0));
            var reserved = BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(2));
            var snapLen = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(4));
            
            var offset = 8;

            string description = "NO_DESCRIPTION";
            string name = "NO_NAME";
            byte[]? macAddress = null; 

            while (offset < buffer.Length) {
                var (success, read) = OptionBlockFactory.TryParse(buffer.Slice(offset), out var option);

                if (!success) {
                    throw new InvalidOperationException("Invalid block size");
                }

                offset += read;

                if (option == null) {
                    // Ignored option 
                    continue;
                }

                if (option is EndOfOption)
                    break;

                if (option is IfMacAddressOption macOption) {
                    macAddress = macOption.MacAddress;
                }

                if (option is StringOptionBlock sb) {
                    if (sb.OptionCode == (int) OptionBlockCode.If_Description) {
                        description = sb.OptionValue; 
                    }

                    if (sb.OptionCode == (int) OptionBlockCode.If_Name) {
                        name = sb.OptionValue;
                    }
                }
            }

            var interfaceDescription = new InterfaceDescription(linkType, 0) {
                Description = description,
                Name = name,
                MacAddress = macAddress
            }; 

            return interfaceDescription;
        }

    }
}