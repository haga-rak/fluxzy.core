using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using Echoes.Encoding.Huffman;
using Echoes.Encoding.Huffman.Interfaces;
using Echoes.Encoding.Utils;

namespace Echoes.Encoding
{
    public class HuffmanCodec
    {
        private readonly IHuffmanDictionary _dictionary;
        private readonly HPackDecodingTree _packDecodingTree;

        public HuffmanCodec()
        : this (HPackDictionary.Instance)
        {

        }

        internal HuffmanCodec(IHuffmanDictionary dictionary)
        {
            _dictionary = dictionary;
            _packDecodingTree = HPackDecodingTree.Default; 
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public int GetEncodedLength(ReadOnlySpan<byte> input)
        {
            var symbols = _dictionary.Symbols;
            var array = input;
            var lengthInBits = 0; 

            for (var index = 0; index < array.Length; index++)
            {
                lengthInBits += symbols[array[index]].LengthBits;
            }
            
            if (lengthInBits % 8 == 0)
                return lengthInBits / 8;

            lengthInBits += 30;

            if (lengthInBits % 8 == 0)
            {
                return lengthInBits / 8; 
            }

            return (lengthInBits / 8) +  1;
        }

        /// <summary>
        /// buffer is used to encode the result 
        /// </summary>
        /// <param name="input"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public Span<byte> Encode(ReadOnlySpan<byte> input, Span<byte> buffer)
        {
            var symbols = _dictionary.Symbols;
            int offsetBits = 0;
            
            for (var index = 0; index < input.Length; index++)
            {
                offsetBits += Write(buffer, offsetBits, symbols[input[index]]);
            }
            
            if (offsetBits % 8 != 0)
            {
                // Padding needed. Insert EOS symbol 
                offsetBits += Write(buffer, offsetBits, symbols[256]);

                if (offsetBits % 8 == 0)
                    return buffer.Slice(0, (offsetBits / 8));

                return buffer.Slice(0, (offsetBits / 8) + 1);
            }

            return buffer.Slice(0, (offsetBits / 8));
        }
        
        /// <summary>
        /// buffer is used to encode the result 
        /// </summary>
        /// <param name="memoryInput"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public Span<byte> Decode(ReadOnlySpan<byte> memoryInput, Span<byte> buffer)
        {
            int currentOffsetBits = 0;
            int totalChar = 0;
        
            Span<byte> destinationBuffer = stackalloc byte[8];

            while (currentOffsetBits < (memoryInput.Length * 8))
            {
                var nextSpan = memoryInput.SliceBitsToNextInt32(currentOffsetBits, destinationBuffer);

                var symbol = _packDecodingTree.Read(nextSpan);

                if (symbol.IsEos)
                    break;

                buffer[totalChar++] = symbol.Key;
                currentOffsetBits += symbol.LengthBits; 
            }

            return buffer.Slice(0, totalChar); 
        }
        

        /// <summary>
        /// Write to a span symbol
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offsetBits"></param>
        /// <param name="symbol"></param>
        /// <returns>Returns the number of bits written on the buffer</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Write(Span<byte> buffer, int offsetBits, Symbol symbol)
        {
            var offsetByte = offsetBits / 8;
            var remain = offsetBits % 8;
            var shiftPad = remain + symbol.LengthBits;
            var destSpan = buffer.Slice(offsetByte);

            if (symbol.LengthBits > 25)
            {
                // Symbol larger than 25 bits can not be encoded with an int. Using a long. 

                var sub = remain == 0 ? 0UL : (0xFFFFFFFF_FFFFFFFF << (sizeof(ulong) * 8 - remain));
                var value = BinaryPrimitives.ReadUInt64BigEndian(destSpan) & sub; 
                var shift = sizeof(ulong) *8  - shiftPad;
                BinaryPrimitives.WriteUInt64BigEndian(destSpan, value | ((ulong) symbol.Value << shift));
            }
            else
            {
                var sub = remain == 0 ? 0U : (0xFFFFFFFF << (sizeof(uint) * 8 - remain));


                var value = BinaryPrimitives.ReadUInt32BigEndian(destSpan) & sub;
                var shift = sizeof(uint) * 8 - shiftPad;
                BinaryPrimitives.WriteUInt32BigEndian(destSpan, value | (symbol.Value << shift));
            }

            return symbol.LengthBits; 
        }
    }
}
