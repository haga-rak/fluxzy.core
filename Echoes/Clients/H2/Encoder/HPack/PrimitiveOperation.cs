using System;
using Echoes.Clients.H2.Encoder.Huffman;

namespace Echoes.Clients.H2.Encoder.HPack
{
    internal class PrimitiveOperation
    {
        private readonly HuffmanCodec _codec;
        private readonly int _maxStringLength;

        public PrimitiveOperation()
            : this (new HuffmanCodec(HPackDictionary.Instance))
        {
        }

        internal PrimitiveOperation(HuffmanCodec codec, int maxStringLength = 1024*16)
        {
            _codec = codec;
            _maxStringLength = maxStringLength;
        }

        public int WriteInt32(Span<byte> output, int value, int prefixSize)
        {
            try
            {
                var maxSize = (1 << prefixSize) - 1;

                if (value < maxSize)
                {
                    // Clear existing value 
                    output[0] = (byte)(output[0] & (0XFF << prefixSize));
                    output[0] |= (byte)((0XFF >> (8 - prefixSize)) & value);

                    return 1;
                }

                var fullPrefix = (0xFF >> (8 - prefixSize));

                output[0] = (byte)(output[0] | fullPrefix);

                value -= fullPrefix;

                int index = 1;

                do
                {
                    var toInsert = value % 0x80;
                    output[index] = (value > 0x7F) ?
                        (byte)(toInsert | (0x80))
                        : (byte)(toInsert & (0x7F));

                    index++;
                    value >>= 7;
                } while (value > 0);

                return index;
            }
            catch (IndexOutOfRangeException)
            {
                throw new HPackCodecException($"Provided buffer is not large enough");
            }
        }

        public int ReadInt32(ReadOnlySpan<byte> input, int prefixSize, out int  value)
        {
            try
            {
                var maxSize = (1 << prefixSize) - 1;
                var firstBlockValue = input[0] & (0xFF >> (8 - prefixSize));

                if (firstBlockValue < maxSize)
                {
                    value = firstBlockValue;
                    return 1;
                }

                int index = 1;
                int result = 0;

                while (index < 6)
                {
                    result |= (input[index] & 0x7F) << (7 * (index - 1));

                    if ((input[index] & 0x80) == 0)
                    {
                        value = result + firstBlockValue;
                        return 1 + index;
                    }

                    index++;
                }

                throw new InvalidOperationException("Integer overflow. Value exceed 28 bits");

            }
            catch (IndexOutOfRangeException)
            {
                throw new HPackCodecException($"Provided buffer is not large enough");
            }
        }


        public int GetStringLength(ReadOnlySpan<byte> input)
        {
            try
            {
                var huffmanEncoded = (input[0] & 0x80) != 0;
                var offset = ReadInt32(input, 7, out var stringLength);

                if (stringLength > _maxStringLength)
                    throw new HPackCodecException(
                        $"string length exceed the maximum authorized : {stringLength} / {_maxStringLength}");

                var rawString = input.Slice(offset, stringLength);

                if (!huffmanEncoded)
                {
                    return stringLength;
                }
                
                return _codec.GetDecodedLength(rawString);

            }
            catch (IndexOutOfRangeException)
            {
                throw new HPackCodecException($"Provided buffer is not large enough");
            }
        }


        public Span<char> ReadString(ReadOnlySpan<byte> input, Span<char> buffer, out int newOffset)
        {
            try
            {
                var huffmanEncoded = (input[0] & 0x80) != 0;
                var offset = ReadInt32(input, 7, out var stringLength);

                if (stringLength > _maxStringLength)
                    throw new HPackCodecException(
                        $"string length exceed the maximum authorized : {stringLength} / {_maxStringLength}");

                var rawString = input.Slice(offset, stringLength);

                if (!huffmanEncoded)
                {
                    var size = System.Text.Encoding.ASCII.GetChars(rawString, buffer);
                    var res = buffer.Slice(0, size);

                    newOffset = stringLength + offset;
                    return res;

                }
                newOffset = stringLength + offset;

                Span<byte> decodeBuffer = stackalloc byte[_maxStringLength];
                var decoded = _codec.Decode(rawString, decodeBuffer);

                var resultLength = System.Text.Encoding.ASCII.GetChars(decoded, buffer);

                return buffer.Slice(0, resultLength);

            }
            catch (IndexOutOfRangeException)
            {
                throw new HPackCodecException($"Provided buffer is not large enough");
            }

        }

        public Span<byte> WriteString(ReadOnlySpan<char> input, Span<byte> buffer, bool huffmanEncoded)
        {
            try
            {
                Span<byte> inputByteBuffer = stackalloc byte[input.Length * 2];
                int size = System.Text.Encoding.ASCII.GetBytes(input, inputByteBuffer);
                Span<byte> inputBytes = inputByteBuffer.Slice(0, size); 


                var encodedLength = _codec.GetEncodedLength(inputBytes);

                huffmanEncoded =  encodedLength < inputBytes.Length;

                buffer[0] = (byte)(huffmanEncoded ? 0x80 : 0);

                var length = !huffmanEncoded ? input.Length : encodedLength;

                var offset = WriteInt32(buffer, length, 7);

                if (huffmanEncoded)
                {
                    var encoded = _codec.Encode(inputBytes, buffer.Slice(offset));
                    return buffer.Slice(0, offset + encoded.Length);
                }

                inputBytes.CopyTo(buffer.Slice(offset));

                return buffer.Slice(0, offset + input.Length);

            }
            catch (IndexOutOfRangeException)
            {
                throw new HPackCodecException($"Provided buffer is not large enough");
            }
        }
        
    }
}