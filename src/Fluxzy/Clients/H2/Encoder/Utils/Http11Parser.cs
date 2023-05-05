// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using Fluxzy.Clients.H2.Encoder.HPack;

namespace Fluxzy.Clients.H2.Encoder.Utils
{
    /// <summary>
    ///     Converts a flat HTTP/1.1 request to a list of (name, value) headers compatible with HTTP/2
    /// </summary>
    public static class Http11Parser
    {
        public static IEnumerable<HeaderField> Read(
            ReadOnlyMemory<char> input, bool isHttps = true,
            bool keepNonForwardableHeader = false,
            bool splitCookies = true)
        {
            var firstLine = true;

            foreach (var line in input.Split(Http11Constants.LineSeparators).ToArray()) {
                if (firstLine) {
                    // parsing request line
                    var arrayOfValue = line.Split(Http11Constants.SpaceSeparators, 3).ToArray();

                    if (arrayOfValue.Length >= 2) {
                        if (arrayOfValue[0].Length >= 4
                            && arrayOfValue[0].Slice(0, 4).Span
                                              .Equals("HTTP".AsSpan(), StringComparison.OrdinalIgnoreCase)) {
                            // Response header block 

                            yield return new HeaderField(Http11Constants.StatusVerb, arrayOfValue[1]);
                        }
                        else {
                            // Request header block

                            yield return new HeaderField(Http11Constants.MethodVerb, arrayOfValue[0]);

                            yield return new HeaderField(Http11Constants.PathVerb,
                                arrayOfValue[1].RemoveProtocolAndAuthority()); // Remove prefix on path

                            yield return new HeaderField(Http11Constants.SchemeVerb,
                                isHttps ? Http11Constants.HttpsVerb : Http11Constants.HttpVerb);

                            if (Http11Constants.SchemeVerb.Span.StartsWith(Http11Constants.HttpsVerb.Span))
                                isHttps = true;
                        }
                    }

                    firstLine = false;

                    continue;
                }

                var kpValue = line.Split(Http11Constants.HeaderSeparator, 2).ToArray();

                if (kpValue.Length != 2)
                    throw new HPackCodecException($"Invalid header on line {line}");

                var headerName = kpValue[0].Trim(); // should we trim here? 

                if (!keepNonForwardableHeader && Http11Constants.NonH2Header.Contains(headerName))
                    continue;

                var headerValue = kpValue[1].Trim();

                if (headerName.Span.Equals(Http11Constants.HostVerb.Span, StringComparison.OrdinalIgnoreCase)) {
                    yield return new HeaderField(Http11Constants.AuthorityVerb, headerValue);

                    continue;
                }

                if (headerName.Span.Equals(Http11Constants.CookieVerb.Span, StringComparison.OrdinalIgnoreCase)) {
                    if (splitCookies) {
                        foreach (var cookieEntry in headerValue.Split(Http11Constants.CookieSeparators)) {
                            yield return new HeaderField(Http11Constants.CookieVerb, cookieEntry.Trim());
                        }

                        continue;
                    }
                }

                yield return new HeaderField(headerName, headerValue);
            }
        }

        public static Span<char> Write(
            ICollection<HeaderField> entries,
            Span<char> buffer)
        {
            char[]? heapBuffer = null;

            try {
                var minimumLength = entries.Sum(s => s.Size) + 64;

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
                var minimumLength = entries.Sum(s => s.Value.Length + s.Name.Length + 32) + 64;

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
            var mapping = entries
                          .Where(t => Http11Constants.ControlHeaders.Contains(t.Name))
                          .ToDictionary
                              (t => t.Name, t => t, SpanCharactersIgnoreCaseComparer.Default);

            var totalWritten = 0;
            var offsetBuffer = buffer;

            if (!mapping.TryGetValue(Http11Constants.MethodVerb, out var method)) {
                if (!mapping.TryGetValue(Http11Constants.StatusVerb, out var statusHeader))
                    throw new HPackCodecException("Invalid HTTP header. Could not find :method or :status");

                // Response header 

                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, "HTTP/1.1 ");
                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, statusHeader.Value.Span);
                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, " ");

                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten,
                    Http11Constants.GetStatusLine(statusHeader.Value).Span);

                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, "\r\n");
            }
            else {
                // Request Header

                if (!mapping.TryGetValue(Http11Constants.PathVerb, out var path))
                    throw new HPackCodecException("Could not find path verb");

                if (!mapping.TryGetValue(Http11Constants.AuthorityVerb, out var authority))
                    throw new HPackCodecException("Could not find authority verb");

                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, method.Value.Span);
                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, ' ');
                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, path.Value.Span);
                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, " HTTP/1.1\r\n");

                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, "Host: ");
                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, authority.Value.Span);
                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, "\r\n");
            }

            foreach (var entry in entries) {
                if (Http11Constants.AvoidAutoParseHttp11Headers.Contains(entry.Name))
                    continue; // PSEUDO headers

                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, entry.Name.Span);
                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, ": ");
                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, entry.Value.Span);
                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, "\r\n");
            }

            var cookieLength = SpanCharsHelper.Join(
                entries.Where(c =>
                    c.Name.Span.Equals(Http11Constants.CookieVerb.Span, StringComparison.OrdinalIgnoreCase)
                ).Select(s => s.Value), "; ".AsSpan(), cookieBuffer);

            var cookieValue = cookieBuffer.Slice(0, cookieLength);

            if (!cookieValue.IsEmpty) {
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
            var mapping = entries
                          .Where(t => Http11Constants.ControlHeaders.Contains(t.Name))
                          .ToDictionary
                              (t => t.Name, t => t, SpanCharactersIgnoreCaseComparer.Default);

            var totalWritten = 0;
            var offsetBuffer = buffer;

            if (!mapping.TryGetValue(Http11Constants.MethodVerb, out var method)) {
                if (!mapping.TryGetValue(Http11Constants.StatusVerb, out var statusHeader))
                    throw new HPackCodecException("Invalid HTTP header. Could not find :method or :status");

                // Response header 

                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, "HTTP/1.1 ");
                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, statusHeader.Value.Span);
                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, " ");

                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten,
                    Http11Constants.GetStatusLine(statusHeader.Value).Span);

                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, "\r\n");
            }
            else {
                // Request Header

                if (!mapping.TryGetValue(Http11Constants.PathVerb, out var path))
                    throw new HPackCodecException("Could not find path verb");

                if (!mapping.TryGetValue(Http11Constants.AuthorityVerb, out var authority))
                    throw new HPackCodecException("Could not find authority verb");

                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, method.Value.Span);
                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, ' ');
                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, path.Value.Span);
                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, " HTTP/1.1\r\n");

                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, "Host: ");
                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, authority.Value.Span);
                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, "\r\n");
            }

            foreach (var entry in entries) {
                if (Http11Constants.AvoidAutoParseHttp11Headers.Contains(entry.Name))
                    continue; // PSEUDO headers

                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, entry.Name.Span);
                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, ": ");
                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, entry.Value.Span);
                SpanCharsHelper.Concat(ref offsetBuffer, ref totalWritten, "\r\n");
            }

            var cookieLength = SpanCharsHelper.Join(
                entries.Where(c =>
                    c.Name.Span.Equals(Http11Constants.CookieVerb.Span, StringComparison.OrdinalIgnoreCase)
                ).Select(s => s.Value), "; ".AsSpan(), cookieBuffer);

            var cookieValue = cookieBuffer.Slice(0, cookieLength);

            if (!cookieValue.IsEmpty) {
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
