// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Text;

namespace Fluxzy.Utils
{
    /// <summary>
    /// Provides utility methods for memory operations.
    /// </summary>
    internal static class MemoryUtility
    {
        /// <summary>
        /// Copies the format and shifts the destination buffer based on the number.
        /// </summary>
        /// <param name="destination">The destination buffer.</param>
        /// <param name="number">The number to format and copy.</param>
        /// <returns>The total number of bytes written to the destination buffer.</returns>
        /// <exception cref="InvalidOperationException">Thrown when number formatting fails.</exception>
        public static int CopyFormatAndShift(ref Span<byte> destination, int number)
        {
            Span<char> numberSpan = stackalloc char[12];

            if (!number.TryFormat(numberSpan, out var charsWritten))
                throw new InvalidOperationException("Number formatting failed"); // Actually, this should never happen 

            return CopyAndShift(ref destination, numberSpan.Slice(0, charsWritten));
        }

        /// <summary>
        /// Copies the contents of a UTF-8 string to a byte span and shifts the destination span accordingly.
        /// </summary>
        /// <param name="destination">The destination span to copy the UTF-8 string to.</param>
        /// <param name="utf8String">The UTF-8 string to copy.</param>
        /// <returns>The number of bytes copied and shifted.</returns>
        public static int CopyAndShift(ref Span<byte> destination, scoped ReadOnlySpan<char> utf8String)
        {
            var utf8Length = Encoding.UTF8.GetByteCount(utf8String);
            Span<byte> utf8Span = stackalloc byte[utf8Length];
            Encoding.UTF8.GetBytes(utf8String, utf8Span);
            return CopyAndShift(ref destination, utf8Span);
        }

        /// <summary>
        /// Copies the specified UTF-8 string and shifts the destination span by the length of the string.
        /// </summary>
        /// <param name="destination">The destination span to copy the UTF-8 string to.</param>
        /// <param name="utf8String">The source UTF-8 string to be copied.</param>
        /// <returns>The number of bytes copied.</returns>
        public static int CopyAndShift(ref Span<byte> destination, scoped ReadOnlySpan<byte> source)
        {
            source.CopyTo(destination);
            destination = destination.Slice(source.Length);
            return source.Length;
        }
    }
}
