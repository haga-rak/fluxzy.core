using System;
using System.Buffers.Binary;

namespace Echoes.Encoding.Huffman
{
    public class Symbol
    {
        public Symbol(uint value, int lengthBits, byte key)
        {
            Value = value;
            LengthBits = lengthBits;
            Key = key;
        }
        public uint Value { get;  } 

        public int LengthBits { get;  }

        public byte Key { get; }

        public bool IsEos => Value == 0x3fffffff;

        public int GetLengthBitsInColumn(int col)
        {
            bool valueContainsByteInColumns = (LengthBits) > (col * 8);

            if (!valueContainsByteInColumns)
                return 0;

            if ((LengthBits / (8 * (col + 1))) >= 1)
                return 8; 

            return LengthBits % 8; 
        }


        /// <summary>
        /// Give all binary variation of the data 
        /// </summary>
        /// <param name="col"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public Span<byte> GetByteVariation(int col, Span<byte> result)
        {
            var numberOfBytesInColumn = GetLengthBitsInColumn(col);

            if (numberOfBytesInColumn == 0)
                return Span<byte>.Empty;

            var referencebyte = GetByte(col);

            var max = (0xFF >> numberOfBytesInColumn) | referencebyte;
            
            int i = 0;
            for (byte currentValue = referencebyte;  currentValue <= max; currentValue++)
            {
                result[i++] = currentValue;

                if (currentValue == max)
                    break;  // byte overflow goes back to 0  ; 
            }

            return result.Slice(0, i);
        }

        public byte GetByte(int col)
        {
            Span<byte> data = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(data, Value << (sizeof(int) * 8 - LengthBits));
            return data[col]; 
        }

        public override string ToString()
        {
            var key = (char) Key;
            var printKey = char.IsControl(key) ? Key.ToString() : key.ToString();
            return $"{printKey} : {Convert.ToString(Value, 2).PadLeft(LengthBits, '0')}";
        }

    }
}