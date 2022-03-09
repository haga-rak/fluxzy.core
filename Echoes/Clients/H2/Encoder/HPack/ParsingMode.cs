using System;

namespace Echoes.Clients.H2.Encoder.HPack
{
    internal static class ParsingMode
    {
        public static HeaderFieldType ParseType(byte firstByte)
        {
            if (firstByte == 0x40)
                return HeaderFieldType.LiteralHeaderFieldIncrementalIndexingWithName;

            if (firstByte == 0)
                return HeaderFieldType.LiteralHeaderFieldWithoutIndexingWithName;

            if (firstByte == 0x10)
                return HeaderFieldType.LiteralHeaderFieldNeverIndexWithName;

            if ((firstByte & 0x80) != 0)
                return HeaderFieldType.IndexedHeaderField;
            
            if ((firstByte & 0x40) != 0)
                return HeaderFieldType.LiteralHeaderFieldIncrementalIndexingExistingName;

            if ((firstByte & 0x20) != 0)
                return HeaderFieldType.DynamicTableSizeUpdate;

            if ((firstByte & 0x10) != 0)
                return HeaderFieldType.LiteralHeaderFieldNeverIndexExistingName;

            if ((firstByte & 0xF0) == 0)
                return HeaderFieldType.LiteralHeaderFieldWithoutIndexingExistingName;



            throw new NotSupportedException($"HPackDecoder could not determine the binary format {firstByte:X}"); 
        }
    }
}