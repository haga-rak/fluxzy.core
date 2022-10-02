// Copyright © 2022 Haga Rakotoharivelo

using Fluxzy.Clients.H2.Encoder.Utils;
using System;
using System.Text;

namespace Fluxzy.Misc
{
    public static class ArrayTextUtilities
    {
        /// <summary>
        /// This method check if first maxCheckLength characters are printable characters
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="maxCheckLength"></param>
        /// <returns></returns>
        public static bool IsText(ReadOnlySpan<byte> buffer, int maxCheckLength = 1024)
        {
            var checkLength = Math.Min(buffer.Length, maxCheckLength);
            var checkedBuffer = buffer.Slice(0, checkLength);
            var charCount = Encoding.UTF8.GetCharCount(checkedBuffer);

            Span<char> charBuffer = stackalloc char[charCount];

            var charResultCount = Encoding.UTF8.GetChars(checkedBuffer, charBuffer);

            charBuffer = charBuffer.Slice(0, charResultCount);

            for (int i = 0; i < charBuffer.Length; i++)
            {
                if (char.IsControl(charBuffer[i]) && !char.IsWhiteSpace(charBuffer[i]))
                    return false; 
            }

            return true;
        }
    }
}