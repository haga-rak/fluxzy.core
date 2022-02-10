using System;
using Echoes.H2.Encoder.HPack;

namespace Echoes.H2.Encoder.Utils
{
    public static class EncodingExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="encoding"></param>
        /// <param name="rawBytes"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        /// <exception cref="HPackCodecException"></exception>
        public static Span<char> GetChars(this System.Text.Encoding encoding, ReadOnlySpan<byte> rawBytes, Span<char> buffer)
        {
            if (rawBytes.IsEmpty)
                return Span<char>.Empty;

            unsafe
            {
                fixed (byte* ptrRawBytes = &rawBytes.GetPinnableReference())
                fixed (char* ptrBuffer = &buffer.GetPinnableReference())
                {
                    encoding.GetDecoder().Convert(ptrRawBytes, rawBytes.Length, ptrBuffer, buffer.Length, true, out var bytesUsed, 
                        out var charUsed, out var completed);

                    if (!completed)
                        throw new HPackCodecException("Unable to decode bytes"); 

                    return buffer.Slice(0, charUsed);
                }
            }
        }

        public static Span<byte> GetBytes(this System.Text.Encoding encoding, ReadOnlySpan<char> rawChars, Span<byte> buffer)
        {
            if (rawChars.IsEmpty)
                return Span<byte>.Empty;

            unsafe
            {
                fixed (char* ptrRawChars = &rawChars.GetPinnableReference())
                fixed (byte* ptrBuffer = &buffer.GetPinnableReference())
                {
                    encoding.GetEncoder().Convert(ptrRawChars, rawChars.Length, ptrBuffer, buffer.Length, true, out var charUsed
                    , out var bytesUsed, out var completed);

                    if (!completed)
                        throw new HPackCodecException("Unable to encode");

                    return buffer.Slice(0, bytesUsed);
                }
            }
        }
    }
}