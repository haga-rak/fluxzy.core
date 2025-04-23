// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using Fluxzy.Clients.H2.Encoder.HPack;

namespace Fluxzy.Clients.H2.Encoder.Utils
{
    internal static class SpanCharsHelper
    {
        private static readonly ReadOnlyMemory<char> HttpsPrefix = "https://".AsMemory();
        private static readonly ReadOnlyMemory<char> EmptyPath = "/".AsMemory();

        public static IEnumerable<ReadOnlyMemory<char>> Split(
            this ReadOnlyMemory<char> input, HashSet<char> separator,
            int numberOfParts = -1)
        {
            var startString = -1;
            var count = 0;

            for (var i = 0; i < input.Length; i++) {
                if (separator.Contains(input.Span[i])) {
                    if (startString == -1)
                        continue;

                    if (numberOfParts >= 0 && count + 1 >= numberOfParts) {
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

            if (startString >= 0) {
                yield return input.Slice(startString);
            }
        }

        public static Span<ReadOnlyMemory<char>> SplitArray(
            this ReadOnlyMemory<char> input, HashSet<char> separator,
            Span<ReadOnlyMemory<char>> buffer, int numberOfParts = -1)
        {
            var count = 0;

            foreach (var item in input.Split(separator, numberOfParts)) {
                if (count >= buffer.Length)
                    throw new HPackCodecException("Unable to split array because provided buffer is not large enough");

                buffer[count++] = item;
            }

            return buffer.Slice(0, count);
        }

        public static ReadOnlyMemory<char> TrimStart(this ReadOnlyMemory<char> input, char @char = ' ')
        {
            var i = 0;

            for (i = 0; i < input.Length; i++) {
                if (input.Span[i] != @char)
                    break;
            }

            return input.Slice(i);
        }

        public static ReadOnlyMemory<char> TrimEnd(this ReadOnlyMemory<char> input, char @char = ' ')
        {
            var i = 0;

            for (i = input.Length - 1; i >= 0; i--) {
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
            if (input.Span.StartsWith(HttpsPrefix.Span, StringComparison.OrdinalIgnoreCase)) {
                var indexOfPath = input.Span.Slice(HttpsPrefix.Length).IndexOf('/');

                if (indexOfPath < 0)
                    return EmptyPath;

                return input.Slice(HttpsPrefix.Length + indexOfPath);
            }

            return input;
        }

        public static int Add(this Span<char> input, in ReadOnlySpan<char> addition)
        {
            addition.CopyTo(input);
            return input.Length;
        }

        public static void Concat(ref Span<char> input, ref int offset, in ReadOnlySpan<char> addition)
        {
            addition.CopyTo(input);
            offset += addition.Length; 
            input = input.Slice(addition.Length);
        }

        public static void Concat(ref Span<char> input, ref int offset, char @char)
        {
            input[0] = @char;
            offset += 1;
            input = input.Slice(1);
        }

        public static int Join(
            in IEnumerable<ReadOnlyMemory<char>> items,
            in ReadOnlySpan<char> separator, Span<char> buffer)
        {
            var first = true;
            var offset = 0;

            var offsetBuffer = buffer;

            foreach (var item in items) {
                if (first) {
                    item.Span.CopyTo(offsetBuffer);
                    offset += item.Length;

                    offsetBuffer = offsetBuffer.Slice(item.Length);

                    first = false;

                    continue;
                }
                
                Concat(ref offsetBuffer, ref offset, separator);
                Concat(ref offsetBuffer, ref offset, item.Span);
            }

            return offset; 
        }

        /// <summary>
        /// Splits the span by the given separator, removing empty segments.
        /// </summary>
        /// <param name="span">The span to split</param>
        /// <param name="separator">The separator to split the span on.</param>
        /// <returns>An enumerator over the span segments.</returns>
        public static StringSplitEnumerator Split(this ReadOnlySpan<char> span, ReadOnlySpan<char> separator) => new(span, separator);

    }

    internal ref struct StringSplitEnumerator
    {
        private readonly ReadOnlySpan<char> _sentinel;
        private ReadOnlySpan<char> _span;

        public StringSplitEnumerator(ReadOnlySpan<char> span, ReadOnlySpan<char> sentinel)
        {
            _span = span;
            _sentinel = sentinel;
        }

        public bool MoveNext()
        {
            if (_span.Length == 0)
            {
                return false;
            }

            var index = _span.IndexOf(_sentinel, StringComparison.Ordinal);
            if (index < 0)
            {
                Current = _span;
                _span = default;
            }
            else
            {
                Current = _span[..index];
                _span = _span[(index + 1)..];
            }

            if (Current.Length == 0)
            {
                return MoveNext();
            }

            return true;
        }

        public ReadOnlySpan<char> Current { readonly get; private set; }

        public readonly StringSplitEnumerator GetEnumerator() => this;
    }

}

