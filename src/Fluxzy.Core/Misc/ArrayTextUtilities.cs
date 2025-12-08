// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers;
using System.IO;
using System.Text;
using Fluxzy.Misc.Streams;

namespace Fluxzy.Misc
{
    public static class ArrayTextUtilities
    {
        /// <summary>
        ///     This method check if first maxCheckLength characters are printable characters
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="maxCheckLength"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static bool IsText(ReadOnlySpan<byte> buffer, int maxCheckLength = 1024, Encoding? encoding = null)
        {
            var checkLength = Math.Min(buffer.Length, maxCheckLength);
            var checkedBuffer = buffer.Slice(0, checkLength);
            var charCount = (encoding ?? Encoding.UTF8).GetCharCount(checkedBuffer);

            var maxStackAllocSize = 1024 * 32;

            char[]? heapCharBuffer = null;

            var charBuffer = charCount < maxStackAllocSize
                ? stackalloc char[charCount]
                : heapCharBuffer = ArrayPool<char>.Shared.Rent(charCount);

            try {
                var charResultCount = (encoding ?? Encoding.UTF8).GetChars(checkedBuffer, charBuffer);

                charBuffer = charBuffer.Slice(0, charResultCount);

                for (var i = 0; i < charBuffer.Length; i++) {
                    if (char.IsControl(charBuffer[i]) && !char.IsWhiteSpace(charBuffer[i]))
                        return false;
                }

                return true;
            }
            finally {
                if (heapCharBuffer != null)
                    ArrayPool<char>.Shared.Return(heapCharBuffer);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="maxCheckLength"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static bool IsText(Stream stream, int maxCheckLength = 1024, Encoding? encoding = null)
        {
            if (!stream.CanSeek)
                throw new ArgumentException("Stream must be seekable");

            byte[]? buffer = null;

            var arrayBuffer = maxCheckLength < 1024
                ? stackalloc byte[maxCheckLength]
                : buffer = ArrayPool<byte>.Shared.Rent(maxCheckLength);

            try {
                var maxRead = stream.ReadMaximum(arrayBuffer);

                return IsText(arrayBuffer.Slice(0, maxRead), maxCheckLength, encoding);
            }
            finally {
                if (buffer != null)
                    ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        public static bool IsText(string fileName, int maxCheckLength = 1024, Encoding? encoding = null)
        {
            using var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            return IsText(stream, maxCheckLength, encoding);
        }
    }
}
