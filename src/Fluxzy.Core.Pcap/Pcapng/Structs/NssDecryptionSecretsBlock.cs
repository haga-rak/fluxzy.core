// // Copyright 2022 - Haga Rakotoharivelo
// 

using System.Buffers.Binary;
using System.Text;

namespace Fluxzy.Core.Pcap.Pcapng.Structs
{
    internal readonly ref struct NssDecryptionSecretsBlock
    {
        public const int BlockTypeValue = 0x0000000A;

        public NssDecryptionSecretsBlock(string nssKey)
        {
            BlockTotalLength = 20;
            SecretsLength = (uint) Encoding.UTF8.GetByteCount(nssKey) ; 
            BlockTotalLength += (int) (SecretsLength + (((4 - SecretsLength % 4) % 4)));
        }

        public uint BlockType => BlockTypeValue;

        public int BlockTotalLength { get; }

        public uint SecretsType => 0x544c534b;

        public uint SecretsLength { get; }

        public int Write(Span<byte> buffer, string nssKey)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(buffer, BlockType);
            BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(4), BlockTotalLength);
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(8), SecretsType);
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(12), SecretsLength);
            
            var length = Encoding.UTF8.GetBytes(nssKey, buffer.Slice(16));

            var offset = 16 + SecretsLength + (((4 - SecretsLength % 4) % 4));

            BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice((int) offset), BlockTotalLength);

            return BlockTotalLength;
        }
    }
}