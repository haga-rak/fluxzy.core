using System;
using System.Collections.Generic;
using Echoes.Clients.H2.Encoder.HPack;

namespace Echoes.Clients.H2.Encoder.Utils
{
    internal static class SpanCharsHelper
    {
        private static readonly ReadOnlyMemory<char> HttpsPrefix = "https://".AsMemory();
        private static readonly ReadOnlyMemory<char> EmptyPath = "/".AsMemory();

        public static IEnumerable<ReadOnlyMemory<char>> Split(this ReadOnlyMemory<char> input, HashSet<char> separator, int numberOfParts = -1)
        {
            int startString = -1;
            int count = 0; 

            for (int i = 0; i < input.Length; i++)
            {
                if (separator.Contains(input.Span[i]))
                {
                    if (startString == -1)
                    {
                        continue;
                    }

                    if (numberOfParts >= 0 && (count + 1) >= numberOfParts)
                    {
                        // Split until the end 
                        yield return input.Slice(startString);
                        yield break; 
                    }

                    count++;
                    // 
                    yield return input.Slice(startString, i - startString);

                    startString = -1;
                    continue;
                }

                if (startString < 0)
                    startString = i;
            }

            if (startString >= 0)
            {
                count++;
                yield return input.Slice(startString);
            }
        }

        public static Span<ReadOnlyMemory<char>> SplitArray(this ReadOnlyMemory<char> input, HashSet<char> separator, Span<ReadOnlyMemory<char>> buffer, int numberOfParts = -1)
        {
            int count = 0;

            foreach (var item in input.Split(separator, numberOfParts))
            {
                if (count >= buffer.Length)
                {
                    throw new HPackCodecException("Unable to split array because provided buffer is not large enough"); 
                }

                buffer[count++] = item; 
            }
            
            return buffer.Slice(0, count);
        }

        public static ReadOnlyMemory<char> TrimStart(this ReadOnlyMemory<char> input, char @char = ' ')
        {
            int i = 0;

            for (i = 0; i < input.Length; i++)
            {
                if (input.Span[i] != @char)
                    break; 
            }

            return input.Slice(i); 
        }

        public static ReadOnlyMemory<char> TrimEnd(this ReadOnlyMemory<char> input, char @char = ' ')
        {
            int i = 0;

            for (i = input.Length - 1; i >= 0; i--)
            {
                if (input.Span[i] != @char)
                    break; 
            }


            return input.Slice(0, i + 1);
        }

        public static ReadOnlyMemory<char> Trim(this ReadOnlyMemory<char> input, char @char = ' ')
        {
            return input.TrimEnd(@char).TrimStart(@char);
        }

        public static ReadOnlyMemory<char> RemoveProtocolAndAuthority(this ReadOnlyMemory<char> input)
        {
            if (input.Span.StartsWith(HttpsPrefix.Span, StringComparison.OrdinalIgnoreCase))
            {
                var indexOfPath = input.Span.Slice(HttpsPrefix.Length).IndexOf('/');

                if (indexOfPath < 0)
                    return EmptyPath;

                return input.Slice(HttpsPrefix.Length + indexOfPath);
            }

            return input; 
        }

        public static Span<char> Concat(this Span<char> input, in ReadOnlySpan<char> addition, ref int offset)
        {
            addition.CopyTo(input);
            offset += addition.Length;
            return input.Slice(addition.Length);
        }

        public static Span<char> Concat(this Span<char> input, char @char, ref int offset)
        {
            input[0] = @char;
            offset += 1; 
            return input.Slice(1);
        }

        public static Span<char> Concat(this Span<char> input, string str, ref int offset)
        {
            str.AsSpan().CopyTo(input);
            offset += str.Length;
            return input.Slice(str.Length);
        }

        public static Span<char> Join(
            in IEnumerable<ReadOnlyMemory<char>> items, 
            in ReadOnlySpan<char> separator, Span<char> buffer)
        {
            bool first = true;
            int offset = 0;

            var offsetBuffer = buffer; 

            foreach (var item in items)
            {
                if (first)
                {
                    item.Span.CopyTo(offsetBuffer);
                    offset += item.Length;

                    offsetBuffer = offsetBuffer.Slice(item.Length); 

                    first = false;
                    continue; 
                }

                offsetBuffer = offsetBuffer.Concat(separator, ref offset);
                offsetBuffer = offsetBuffer.Concat(item.Span, ref offset); 
            }

            return buffer.Slice(0, offset);
        }
    }



}