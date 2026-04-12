// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers;
using System.Collections.Generic;
using Fluxzy.Clients.H2.Encoder.HPack;

namespace Fluxzy.Clients.H2.Encoder.Utils
{
    /// <summary>
    ///     Converts a flat HTTP/1.1 request to a list of (name, value) headers compatible with HTTP/2
    /// </summary>
    public static class Http11Parser
    {
        public static List<HeaderField> Read(
            ReadOnlyMemory<char> input, bool isHttps = true,
            bool keepNonForwardableHeader = false,
            bool splitCookies = true)
        {
            var result = new List<HeaderField>();
            var reader = new Http11HeaderReader(input, isHttps, keepNonForwardableHeader, splitCookies);

            while (reader.MoveNext())
                result.Add(reader.Current);

            return result;
        }

        public static Span<char> Write(
            ICollection<HeaderField> entries,
            Span<char> buffer)
        {
            char[]? heapBuffer = null;

            try {
                var minimumLength = 64;

                foreach (var entry in entries)
                    minimumLength += entry.Size;

                var cookieBuffer = minimumLength < 1024
                    ? stackalloc char[minimumLength]
                    : heapBuffer = ArrayPool<char>.Shared.Rent(minimumLength);

                var length = InternalWrite(entries, buffer, cookieBuffer);

                return buffer.Slice(0, length);
            }
            finally {
                if (heapBuffer != null)
                    ArrayPool<char>.Shared.Return(heapBuffer);
            }
        }

        /// <summary>
        ///     TODO bechn introduce base interface vs unboxing cost
        /// </summary>
        /// <param name="entries"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static Span<char> Write(
            ICollection<HeaderFieldInfo> entries,
            Span<char> buffer)
        {
            char[]? heapBuffer = null;

            try {
                var minimumLength = 64;

                foreach (var entry in entries)
                    minimumLength += entry.Value.Length + entry.Name.Length + 32;

                var cookieBuffer = minimumLength < 1024
                    ? stackalloc char[minimumLength]
                    : heapBuffer = ArrayPool<char>.Shared.Rent(minimumLength);

                var length = InternalWrite(entries, buffer, cookieBuffer);

                return buffer.Slice(0, length);
            }
            finally {
                if (heapBuffer != null)
                    ArrayPool<char>.Shared.Return(heapBuffer);
            }
        }

        private static int InternalWrite(
            in ICollection<HeaderField> entries, in Span<char> buffer,
            in Span<char> cookieBuffer)
        {
            // Linear scan for control headers (max 5) — avoids Dictionary + LINQ allocations
            var method = default(HeaderField);
            var status = default(HeaderField);
            var path = default(HeaderField);
            var authority = default(HeaderField);
            var hasMethod = false;
            var hasStatus = false;
            var hasPath = false;
            var hasAuthority = false;

            foreach (var entry in entries) {
                var name = entry.Name;

                if (name.Span.Equals(Http11Constants.MethodVerb.Span, StringComparison.OrdinalIgnoreCase)) {
                    method = entry;
                    hasMethod = true;
                }
                else if (name.Span.Equals(Http11Constants.StatusVerb.Span, StringComparison.OrdinalIgnoreCase)) {
                    status = entry;
                    hasStatus = true;
                }
                else if (name.Span.Equals(Http11Constants.PathVerb.Span, StringComparison.OrdinalIgnoreCase)) {
                    path = entry;
                    hasPath = true;
                }
                else if (name.Span.Equals(Http11Constants.AuthorityVerb.Span, StringComparison.OrdinalIgnoreCase)) {
                    authority = entry;
                    hasAuthority = true;
                }
            }

            var totalWritten = 0;
            var offsetBuffer = buffer;

            if (!hasMethod) {
                if (!hasStatus)
                    throw new HPackCodecException("Invalid HTTP header. Could not find :method or :status");

                // Response header

                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, "HTTP/1.1 ");
                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, status.Value.Span);
                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, " ");

                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten,
                    Http11Constants.GetStatusLine(status.Value).Span);

                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, "\r\n");
            }
            else {
                // Request Header

                if (!hasPath)
                    throw new HPackCodecException("Could not find path verb");

                if (!hasAuthority)
                    throw new HPackCodecException("Could not find authority verb");

                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, method.Value.Span);
                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, ' ');
                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, path.Value.Span);
                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, " HTTP/1.1\r\n");

                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, "Host: ");
                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, authority.Value.Span);
                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, "\r\n");
            }

            // Write non-pseudo headers and join cookies in a single pass
            var cookieOffset = 0;
            var cookieOffsetBuffer = cookieBuffer;
            var hasCookie = false;

            foreach (var entry in entries) {
                if (entry.Name.Span.Equals(Http11Constants.CookieVerb.Span, StringComparison.OrdinalIgnoreCase)) {
                    // Accumulate cookie values with "; " separator
                    if (hasCookie) {
                        SpanCharsHelper.Concat(ref cookieOffsetBuffer, ref cookieOffset, "; ");
                    }

                    SpanCharsHelper.Concat(ref cookieOffsetBuffer, ref cookieOffset, entry.Value.Span);
                    hasCookie = true;

                    continue;
                }

                if (Http11Constants.AvoidAutoParseHttp11Headers.Contains(entry.Name))
                    continue; // PSEUDO headers

                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, entry.Name.Span);
                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, ": ");
                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, entry.Value.Span);
                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, "\r\n");
            }

            if (hasCookie) {
                var cookieValue = cookieBuffer.Slice(0, cookieOffset);
                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, Http11Constants.CookieVerb.Span);
                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, ": ");
                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, cookieValue);
                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, "\r\n");
            }

            SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, "\r\n");

            return totalWritten;
        }

        private static int InternalWrite(
            in ICollection<HeaderFieldInfo> entries, in Span<char> buffer,
            in Span<char> cookieBuffer)
        {
            // Linear scan for control headers — avoids Dictionary + LINQ allocations
            var method = default(HeaderFieldInfo);
            var status = default(HeaderFieldInfo);
            var path = default(HeaderFieldInfo);
            var authority = default(HeaderFieldInfo);
            var hasMethod = false;
            var hasStatus = false;
            var hasPath = false;
            var hasAuthority = false;

            foreach (var entry in entries) {
                var name = entry.Name;

                if (name.Span.Equals(Http11Constants.MethodVerb.Span, StringComparison.OrdinalIgnoreCase)) {
                    method = entry;
                    hasMethod = true;
                }
                else if (name.Span.Equals(Http11Constants.StatusVerb.Span, StringComparison.OrdinalIgnoreCase)) {
                    status = entry;
                    hasStatus = true;
                }
                else if (name.Span.Equals(Http11Constants.PathVerb.Span, StringComparison.OrdinalIgnoreCase)) {
                    path = entry;
                    hasPath = true;
                }
                else if (name.Span.Equals(Http11Constants.AuthorityVerb.Span, StringComparison.OrdinalIgnoreCase)) {
                    authority = entry;
                    hasAuthority = true;
                }
            }

            var totalWritten = 0;
            var offsetBuffer = buffer;

            if (!hasMethod) {
                if (!hasStatus)
                    throw new HPackCodecException("Invalid HTTP header. Could not find :method or :status");

                // Response header

                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, "HTTP/1.1 ");
                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, status.Value.Span);
                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, " ");

                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten,
                    Http11Constants.GetStatusLine(status.Value).Span);

                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, "\r\n");
            }
            else {
                // Request Header

                if (!hasPath)
                    throw new HPackCodecException("Could not find path verb");

                if (!hasAuthority)
                    throw new HPackCodecException("Could not find authority verb");

                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, method.Value.Span);
                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, ' ');
                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, path.Value.Span);
                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, " HTTP/1.1\r\n");

                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, "Host: ");
                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, authority.Value.Span);
                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, "\r\n");
            }

            // Write non-pseudo headers and join cookies in a single pass
            var cookieOffset = 0;
            var cookieOffsetBuffer = cookieBuffer;
            var hasCookie = false;

            foreach (var entry in entries) {
                if (entry.Name.Span.Equals(Http11Constants.CookieVerb.Span, StringComparison.OrdinalIgnoreCase)) {
                    if (hasCookie) {
                        SpanCharsHelper.Concat(ref cookieOffsetBuffer, ref cookieOffset, "; ");
                    }

                    SpanCharsHelper.Concat(ref cookieOffsetBuffer, ref cookieOffset, entry.Value.Span);
                    hasCookie = true;

                    continue;
                }

                if (Http11Constants.AvoidAutoParseHttp11Headers.Contains(entry.Name))
                    continue; // PSEUDO headers

                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, entry.Name.Span);
                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, ": ");
                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, entry.Value.Span);
                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, "\r\n");
            }

            if (hasCookie) {
                var cookieValue = cookieBuffer.Slice(0, cookieOffset);
                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, Http11Constants.CookieVerb.Span);
                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, ": ");
                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, cookieValue);
                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, "\r\n");
            }

            SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, "\r\n");

            return totalWritten;
        }
    }
}
