// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using Fluxzy.Clients.H2.Encoder.HPack;

namespace Fluxzy.Clients.H2.Encoder.Utils
{
    /// <summary>
    ///     Allocation-free, forward-only reader that walks an HTTP/1.1 header block and
    ///     yields HTTP/2-compatible <see cref="HeaderField" /> entries.
    ///
    ///     Replaces the list-and-iterator pair used by the previous
    ///     <see cref="Http11Parser.Read" /> implementation on the hot HPACK encoding path.
    /// </summary>
    internal ref struct Http11HeaderReader
    {
        private readonly ReadOnlyMemory<char> _source;
        private readonly bool _keepNonForwardableHeader;
        private readonly bool _splitCookies;

        // Offset into _source where line parsing resumes.
        private int _sourcePos;

        // Pseudo-headers produced from the first line (request: method/scheme/path; response: status).
        private int _pendingCount;
        private int _pendingIndex;
        private HeaderField _pending0;
        private HeaderField _pending1;
        private HeaderField _pending2;

        // Mid-cookie split state: offsets into _source of the remaining cookie value.
        private int _cookieRemainingStart;
        private int _cookieRemainingEnd;
        private bool _splittingCookie;

        public Http11HeaderReader(
            ReadOnlyMemory<char> source,
            bool isHttps = true,
            bool keepNonForwardableHeader = false,
            bool splitCookies = true)
        {
            _source = source;
            _keepNonForwardableHeader = keepNonForwardableHeader;
            _splitCookies = splitCookies;
            _sourcePos = 0;
            _pendingIndex = 0;
            _pendingCount = 0;
            _pending0 = default;
            _pending1 = default;
            _pending2 = default;
            _cookieRemainingStart = 0;
            _cookieRemainingEnd = 0;
            _splittingCookie = false;
            Current = default;

            ParseFirstLine(isHttps);
        }

        public HeaderField Current { get; private set; }

        public readonly Http11HeaderReader GetEnumerator() => this;

        public bool MoveNext()
        {
            // 1) Serve any queued pseudo-headers produced by the first line.
            if (_pendingIndex < _pendingCount) {
                Current = _pendingIndex switch {
                    0 => _pending0,
                    1 => _pending1,
                    2 => _pending2,
                    _ => default
                };
                _pendingIndex++;
                return true;
            }

            // 2) Continue a cookie split if we paused mid-line on the previous call.
            if (_splittingCookie) {
                if (TryNextCookie(out var cookie)) {
                    Current = cookie;
                    return true;
                }

                _splittingCookie = false;
            }

            // 3) Walk remaining header lines.
            var span = _source.Span;

            while (_sourcePos < span.Length) {
                var lineStart = SkipEmptyLines(span, _sourcePos);

                if (lineStart >= span.Length) {
                    _sourcePos = span.Length;
                    break;
                }

                var lineEnd = FindLineEnd(span, lineStart);
                _sourcePos = lineEnd;

                if (lineStart >= lineEnd)
                    continue;

                if (TryEmitHeaderLine(span, lineStart, lineEnd, out var field)) {
                    Current = field;
                    return true;
                }
            }

            Current = default;
            return false;
        }

        private bool TryEmitHeaderLine(
            ReadOnlySpan<char> span, int lineStart, int lineEnd, out HeaderField field)
        {
            var lineLength = lineEnd - lineStart;
            var lineSpan = span.Slice(lineStart, lineLength);

            var colonRelative = lineSpan.IndexOf(':');

            if (colonRelative < 0) {
                throw new HPackCodecException(
                    $"Invalid header on line {_source.Slice(lineStart, lineLength)}");
            }

            var nameStart = lineStart;
            var nameEnd = lineStart + colonRelative;

            // Trim spaces around the name. HTTP/1.1 does not permit whitespace in a field-name,
            // but we stay lenient to preserve the previous parser's tolerance.
            while (nameStart < nameEnd && span[nameStart] == ' ')
                nameStart++;

            while (nameEnd > nameStart && span[nameEnd - 1] == ' ')
                nameEnd--;

            if (nameEnd == nameStart) {
                throw new HPackCodecException(
                    $"Invalid header on line {_source.Slice(lineStart, lineLength)}");
            }

            var valueStart = lineStart + colonRelative + 1;
            var valueEnd = lineEnd;

            // Trim spaces (OWS) around the value. Matches the previous Trim(' ') behavior.
            while (valueStart < valueEnd && span[valueStart] == ' ')
                valueStart++;

            while (valueEnd > valueStart && span[valueEnd - 1] == ' ')
                valueEnd--;

            var nameSpan = span.Slice(nameStart, nameEnd - nameStart);

            if (!_keepNonForwardableHeader && IsNonForwardable(nameSpan)) {
                field = default;
                return false;
            }

            // Host → :authority
            if (nameSpan.Equals(Http11Constants.HostVerb.Span, StringComparison.OrdinalIgnoreCase)) {
                field = new HeaderField(
                    Http11Constants.AuthorityVerb,
                    _source.Slice(valueStart, valueEnd - valueStart));

                return true;
            }

            // Cookie → optionally split on ';'
            if (_splitCookies &&
                nameSpan.Equals(Http11Constants.CookieVerb.Span, StringComparison.OrdinalIgnoreCase)) {
                _cookieRemainingStart = valueStart;
                _cookieRemainingEnd = valueEnd;
                _splittingCookie = true;

                if (TryNextCookie(out field))
                    return true;

                _splittingCookie = false;
                field = default;

                return false;
            }

            field = new HeaderField(
                _source.Slice(nameStart, nameEnd - nameStart),
                _source.Slice(valueStart, valueEnd - valueStart));

            return true;
        }

        private bool TryNextCookie(out HeaderField cookie)
        {
            var span = _source.Span;

            while (_cookieRemainingStart < _cookieRemainingEnd) {
                // Skip leading separators/whitespace between cookies.
                while (_cookieRemainingStart < _cookieRemainingEnd &&
                       (span[_cookieRemainingStart] == ' ' ||
                        span[_cookieRemainingStart] == ';')) {
                    _cookieRemainingStart++;
                }

                if (_cookieRemainingStart >= _cookieRemainingEnd)
                    break;

                var entryStart = _cookieRemainingStart;
                var remainingSlice = span.Slice(entryStart, _cookieRemainingEnd - entryStart);
                var sepRelative = remainingSlice.IndexOf(';');

                int entryEnd;

                if (sepRelative < 0) {
                    entryEnd = _cookieRemainingEnd;
                    _cookieRemainingStart = _cookieRemainingEnd;
                }
                else {
                    entryEnd = entryStart + sepRelative;
                    _cookieRemainingStart = entryEnd + 1;
                }

                // Trim trailing spaces.
                while (entryEnd > entryStart && span[entryEnd - 1] == ' ')
                    entryEnd--;

                if (entryEnd > entryStart) {
                    cookie = new HeaderField(
                        Http11Constants.CookieVerb,
                        _source.Slice(entryStart, entryEnd - entryStart));

                    return true;
                }
            }

            cookie = default;

            return false;
        }

        private void ParseFirstLine(bool isHttps)
        {
            var span = _source.Span;

            if (span.IsEmpty)
                return;

            var lineStart = SkipEmptyLines(span, 0);

            if (lineStart >= span.Length) {
                _sourcePos = span.Length;

                return;
            }

            var lineEnd = FindLineEnd(span, lineStart);
            _sourcePos = lineEnd;

            if (lineStart >= lineEnd)
                return;

            var line = _source.Slice(lineStart, lineEnd - lineStart);
            var lineSpan = line.Span;

            // Response header block starts with "HTTP" (e.g. "HTTP/1.1 200 OK").
            if (lineSpan.Length >= 4 &&
                lineSpan.Slice(0, 4).Equals("HTTP".AsSpan(), StringComparison.OrdinalIgnoreCase)) {
                ParseResponseStatus(line);

                return;
            }

            ParseRequestLine(line, isHttps);
        }

        private void ParseRequestLine(ReadOnlyMemory<char> line, bool isHttps)
        {
            var lineSpan = line.Span;

            var firstSpace = lineSpan.IndexOfAny(' ', '\t');

            if (firstSpace <= 0)
                return;

            var pathStart = firstSpace + 1;

            while (pathStart < lineSpan.Length &&
                   (lineSpan[pathStart] == ' ' || lineSpan[pathStart] == '\t')) {
                pathStart++;
            }

            if (pathStart >= lineSpan.Length)
                return;

            var tail = lineSpan.Slice(pathStart);
            var pathTerminator = tail.IndexOfAny(' ', '\t');
            var pathLength = pathTerminator < 0 ? tail.Length : pathTerminator;

            if (pathLength == 0)
                return;

            var methodMem = line.Slice(0, firstSpace);
            var pathMem = line.Slice(pathStart, pathLength).RemoveProtocolAndAuthority();

            _pending0 = new HeaderField(Http11Constants.MethodVerb, methodMem);
            _pending1 = new HeaderField(
                Http11Constants.SchemeVerb,
                isHttps ? Http11Constants.HttpsVerb : Http11Constants.HttpVerb);
            _pending2 = new HeaderField(Http11Constants.PathVerb, pathMem);
            _pendingCount = 3;
        }

        private void ParseResponseStatus(ReadOnlyMemory<char> line)
        {
            var lineSpan = line.Span;

            var firstSpace = lineSpan.IndexOfAny(' ', '\t');

            if (firstSpace < 0)
                return;

            var statusStart = firstSpace + 1;

            while (statusStart < lineSpan.Length &&
                   (lineSpan[statusStart] == ' ' || lineSpan[statusStart] == '\t')) {
                statusStart++;
            }

            if (statusStart >= lineSpan.Length)
                return;

            var tail = lineSpan.Slice(statusStart);
            var statusTerminator = tail.IndexOfAny(' ', '\t');
            var statusLength = statusTerminator < 0 ? tail.Length : statusTerminator;

            if (statusLength == 0)
                return;

            _pending0 = new HeaderField(
                Http11Constants.StatusVerb,
                line.Slice(statusStart, statusLength));

            _pendingCount = 1;
        }

        private static int SkipEmptyLines(ReadOnlySpan<char> span, int from)
        {
            while (from < span.Length && (span[from] == '\r' || span[from] == '\n'))
                from++;

            return from;
        }

        private static int FindLineEnd(ReadOnlySpan<char> span, int from)
        {
            if (from >= span.Length)
                return span.Length;

            var relative = span.Slice(from).IndexOfAny('\r', '\n');

            return relative < 0 ? span.Length : from + relative;
        }

        /// <summary>
        ///     Inlined lookup for the small NonH2Header set. Avoids the per-call hashset hash
        ///     cost that dominates the previous parser on the hot encode path.
        /// </summary>
        private static bool IsNonForwardable(ReadOnlySpan<char> name)
        {
            // Known names (case-insensitive): connection, keep-alive, proxy-authenticate,
            // trailer, upgrade, alt-svc, expect, x-fluxzy-live-edit.
            return name.Length switch {
                6 => name.Equals("expect", StringComparison.OrdinalIgnoreCase),
                7 => name.Equals("trailer", StringComparison.OrdinalIgnoreCase)
                     || name.Equals("upgrade", StringComparison.OrdinalIgnoreCase)
                     || name.Equals("alt-svc", StringComparison.OrdinalIgnoreCase),
                10 => name.Equals("connection", StringComparison.OrdinalIgnoreCase)
                      || name.Equals("keep-alive", StringComparison.OrdinalIgnoreCase),
                18 => name.Equals("proxy-authenticate", StringComparison.OrdinalIgnoreCase)
                      || name.Equals("x-fluxzy-live-edit", StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        }
    }
}
