// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers;
using Fluxzy.Clients.H2.Encoder.Huffman;

namespace Fluxzy.Clients.H2.Encoder.HPack
{
    internal class PrimitiveOperation
    {
        private readonly HuffmanCodec _codec;
        private readonly int _maxStringLength;

        public PrimitiveOperation()
            : this(new HuffmanCodec(HPackDictionary.Instance))
        {
        }

        internal PrimitiveOperation(HuffmanCodec codec, int maxStringLength = 1024 * 128)
        {
            _codec = codec;
            _maxStringLength = maxStringLength;
        }

        public int WriteInt32(Span<byte> output, int value, int prefixSize)
        {
            try {
                var maxSize = (1 << prefixSize) - 1;

                if (value < maxSize) {
                    // Clear existing value 
                    output[0] = (byte) (output[0] & (0XFF << prefixSize));
                    output[0] |= (byte) ((0XFF >> (8 - prefixSize)) & value);

                    return 1;
                }

                var fullPrefix = 0xFF >> (8 - prefixSize);

                output[0] = (byte) (output[0] | fullPrefix);

                value -= fullPrefix;

                var index = 1;

                do {
                    var toInsert = value % 0x80;

                    output[index] = value > 0x7F
                        ? (byte) (toInsert | 0x80)
                        : (byte) (toInsert & 0x7F);

                    index++;
                    value >>= 7;
                }
                while (value > 0);

                return index;
            }
            catch (IndexOutOfRangeException) {
                throw new HPackCodecException("Provided buffer is not large enough");
            }
        }

        public int ReadInt32(ReadOnlySpan<byte> input, int prefixSize, out int value)
        {
            try {
                var maxSize = (1 << prefixSize) - 1;
                var firstBlockValue = input[0] & (0xFF >> (8 - prefixSize));

                if (firstBlockValue < maxSize) {
                    value = firstBlockValue;

                    return 1;
                }

                var index = 1;
                var result = 0;

                while (index < 6) {
                    result |= (input[index] & 0x7F) << (7 * (index - 1));

                    if ((input[index] & 0x80) == 0) {
                        value = result + firstBlockValue;

                        return 1 + index;
                    }

                    index++;
                }

                throw new InvalidOperationException("Integer overflow. Value exceed 28 bits");
            }
            catch (IndexOutOfRangeException) {
                throw new HPackCodecException("Provided buffer is not large enough");
            }
        }

        public int GetStringLength(ReadOnlySpan<byte> input)
        {
            try {
                var huffmanEncoded = (input[0] & 0x80) != 0;
                var offset = ReadInt32(input, 7, out var stringLength);

                if (stringLength > _maxStringLength) {
                    throw new HPackCodecException(
                        $"string length exceed the maximum authorized : {stringLength} / {_maxStringLength}");
                }

                var rawString = input.Slice(offset, stringLength);

                if (!huffmanEncoded)
                    return stringLength;

                return _codec.GetDecodedLength(rawString);
            }
            catch (IndexOutOfRangeException) {
                throw new HPackCodecException("Provided buffer is not large enough");
            }
        }

        /// <summary>
        ///     Reads the string wire prefix (huffman flag and wire byte length) without any Huffman decoding.
        ///     Returns the number of prefix bytes consumed from input.
        /// </summary>
        public int ReadStringPrefix(ReadOnlySpan<byte> input, out int wireLength, out bool isHuffman)
        {
            isHuffman = (input[0] & 0x80) != 0;
            var prefixBytes = ReadInt32(input, 7, out wireLength);

            if (wireLength > _maxStringLength) {
                throw new HPackCodecException(
                    $"string length exceed the maximum authorized : {wireLength} / {_maxStringLength}");
            }

            return prefixBytes;
        }

        public Span<char> ReadString(ReadOnlySpan<byte> input, Span<char> buffer, out int newOffset)
        {
            try {
                var huffmanEncoded = (input[0] & 0x80) != 0;
                var offset = ReadInt32(input, 7, out var stringLength);

                if (stringLength > _maxStringLength) {
                    throw new HPackCodecException(
                        $"string length exceed the maximum authorized : {stringLength} / {_maxStringLength}");
                }

                var rawString = input.Slice(offset, stringLength);
                newOffset = stringLength + offset;

                if (!huffmanEncoded) {
                    // Direct byte-to-char widening (HPACK strings are ASCII)
                    for (var i = 0; i < rawString.Length; i++)
                        buffer[i] = (char) rawString[i];

                    return buffer.Slice(0, rawString.Length);
                }

                // Upper bound for Huffman: shortest code is 5 bits, so max decoded = wireLen * 8/5 < wireLen * 2
                var maxDecodedLength = stringLength * 2;

                byte[]? heapBuffer = null;

                var decodeBuffer = maxDecodedLength < 1024
                    ? stackalloc byte[maxDecodedLength]
                    : heapBuffer = ArrayPool<byte>.Shared.Rent(maxDecodedLength);

                try {
                    var decoded = _codec.Decode(rawString, decodeBuffer);

                    // Direct byte-to-char widening (HPACK strings are ASCII)
                    for (var i = 0; i < decoded.Length; i++)
                        buffer[i] = (char) decoded[i];

                    return buffer.Slice(0, decoded.Length);
                }
                finally {
                    if (heapBuffer != null)
                        ArrayPool<byte>.Shared.Return(heapBuffer);
                }
            }
            catch (IndexOutOfRangeException) {
                throw new HPackCodecException("Provided buffer is not large enough");
            }
        }

        public Span<byte> WriteString(ReadOnlySpan<char> input, Span<byte> buffer, bool huffmanEncoded)
        {
            byte[]? heapBuffer = null;

            try {
                var inputByteBuffer = input.Length * 2 < 1024
                    ? stackalloc byte[input.Length * 2]
                    : heapBuffer = ArrayPool<byte>.Shared.Rent(input.Length * 2);

                // Direct char-to-byte narrowing (HPACK strings are ASCII)
                for (var i = 0; i < input.Length; i++)
                    inputByteBuffer[i] = (byte) input[i];

                var inputBytes = inputByteBuffer.Slice(0, input.Length);

                var encodedLength = _codec.GetEncodedLength(inputBytes);

                huffmanEncoded = encodedLength < inputBytes.Length;

                buffer[0] = (byte) (huffmanEncoded ? 0x80 : 0);

                var length = !huffmanEncoded ? input.Length : encodedLength;

                var offset = WriteInt32(buffer, length, 7);

                if (huffmanEncoded) {
                    var encoded = _codec.Encode(inputBytes, buffer.Slice(offset));

                    return buffer.Slice(0, offset + encoded.Length);
                }

                inputBytes.CopyTo(buffer.Slice(offset));

                return buffer.Slice(0, offset + input.Length);
            }
            catch (IndexOutOfRangeException) {
                throw new HPackCodecException("Provided buffer is not large enough");
            }
            finally {
                if (heapBuffer != null)
                    ArrayPool<byte>.Shared.Return(heapBuffer);
            }
        }
    }
}
