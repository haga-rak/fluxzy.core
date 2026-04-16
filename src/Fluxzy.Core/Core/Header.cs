// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fluxzy.Clients.H2.Encoder;
using Fluxzy.Clients.H2.Encoder.Utils;
using Fluxzy.Misc.ResizableBuffers;

namespace Fluxzy.Core
{
    public abstract class Header
    {
        protected static readonly byte[] CloseFlatHeader = "Connection: close\r\n"u8.ToArray();
        protected static readonly byte[] KeepAliveFlatHeader = "Connection: keep-alive\r\n"u8.ToArray();

        private readonly List<HeaderField> _rawHeaderFields;

        protected Header(IEnumerable<HeaderField> headerFields)
        {
            _rawHeaderFields = headerFields as List<HeaderField> ?? new List<HeaderField>(headerFields);

            // Single pass to compute ChunkedBody and ContentLength
            var contentLength = -1L;

            foreach (var field in _rawHeaderFields) {
                if (!ChunkedBody &&
                    field.Name.Span.Equals(Http11Constants.TransferEncodingVerb.Span,
                        StringComparison.OrdinalIgnoreCase) &&
                    field.Value.Span.Equals("chunked", StringComparison.OrdinalIgnoreCase)) {
                    ChunkedBody = true;
                }

                if (field.Name.Span.Equals(Http11Constants.ContentLength.Span,
                        StringComparison.OrdinalIgnoreCase) &&
                    long.TryParse(field.Value.Span, out contentLength)) {
                    // In case of multiple content length we take the last
                    ContentLength = contentLength;
                }
            }
        }

        protected Header(
            ReadOnlyMemory<char> rawHeader,
            bool isSecure)
            : this(Http11Parser.Read(rawHeader, isSecure, true, false))
        {
        }

        public IEnumerable<HeaderField> this[ReadOnlyMemory<char> key] {
            get {
                foreach (var field in _rawHeaderFields) {
                    if (field.Name.Span.Equals(key.Span, StringComparison.OrdinalIgnoreCase))
                        yield return field;
                }
            }
        }

        public IEnumerable<HeaderField> this[string headerName] => this[headerName.AsMemory()];

        protected bool TryGetFirstHeader(ReadOnlyMemory<char> name, out HeaderField field)
        {
            foreach (var f in _rawHeaderFields) {
                if (f.Name.Span.Equals(name.Span, StringComparison.OrdinalIgnoreCase)) {
                    field = f;
                    return true;
                }
            }

            field = default;
            return false;
        }

        protected bool TryGetLastHeader(ReadOnlyMemory<char> name, out HeaderField field)
        {
            field = default;
            var found = false;

            foreach (var f in _rawHeaderFields) {
                if (f.Name.Span.Equals(name.Span, StringComparison.OrdinalIgnoreCase)) {
                    field = f;
                    found = true;
                }
            }

            return found;
        }

        protected bool HasHeaderValueEqualsAny(ReadOnlyMemory<char> name, string value1, string? value2 = null)
        {
            foreach (var f in _rawHeaderFields) {
                if (!f.Name.Span.Equals(name.Span, StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }

                var valSpan = f.Value.Span;

                if (valSpan.Equals(value1, StringComparison.OrdinalIgnoreCase)) {
                    return true;
                }

                if (value2 != null && valSpan.Equals(value2, StringComparison.OrdinalIgnoreCase)) {
                    return true;
                }
            }

            return false;
        }

        protected bool HasHeaderValueContains(ReadOnlyMemory<char> name, string value)
        {
            foreach (var f in _rawHeaderFields) {
                if (f.Name.Span.Equals(name.Span, StringComparison.OrdinalIgnoreCase) &&
                    f.Value.Span.Contains(value, StringComparison.OrdinalIgnoreCase)) {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     If transfer-encoding chunked is defined
        /// </summary>
        public bool ChunkedBody { get; private set; }

        /// <summary>
        ///     Content length of body. -1 if undefined
        /// </summary>
        public long ContentLength { get; set; } = -1;

        /// <summary>
        ///     Returns all headers, including non-forwardable
        /// </summary>
        public IReadOnlyCollection<HeaderField> HeaderFields => _rawHeaderFields;

        /// <summary>
        ///     Returns all headers, excluding non-forwardable
        /// </summary>
        public IEnumerable<HeaderField> Headers {
            get
            {
                foreach (var header in _rawHeaderFields) {
                    if (Http11Constants.IsNonForwardableHeader(header.Name)) {
                        continue;
                    }

                    yield return header;
                }
            }
        }

        internal void AddExtraHeaderFieldToLocalConnection(HeaderField headerField)
        {
            _rawHeaderFields.Add(headerField);
        }

        public void AltDeleteHeader(string name)
        {
            _rawHeaderFields.RemoveAll(h => h.Name.Span.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public void AltAddHeader(string name, string value)
        {
            _rawHeaderFields.Add(new HeaderField(name, value));
        }

        public void AltReplaceHeaders(string name, string value, bool addIfAbsent, string? appendSeparator = null)
        {
            var replaceHeaders = _rawHeaderFields.Where(r => r.Name.Span.Equals(name,
                StringComparison.OrdinalIgnoreCase)).ToList();

            var exist = _rawHeaderFields.RemoveAll(r => r.Name.Span.Equals(name,
                StringComparison.OrdinalIgnoreCase)) > 0;

            foreach (var replaceHeader in replaceHeaders) {
                var previousValue = replaceHeader.Value;
                var previousValueString = previousValue.ToString();

                var finalValue = value.Replace($"{{{{previous}}}}{appendSeparator ?? string.Empty}",
                    previousValueString);

                if (finalValue == value && appendSeparator != null) {
                    finalValue += appendSeparator;
                }

                var replacement = new HeaderField(
                    replaceHeader.Name,
                    finalValue.AsMemory()
                );

                _rawHeaderFields.Add(replacement);
            }

            if (addIfAbsent && !exist) {
                value = value.Replace("{{previous}}", "");

                var appendedHeader = new HeaderField(
                    name,
                    value
                );

                _rawHeaderFields.Add(appendedHeader);
            }
        }

        public void RemoveHeader(string headerName)
        {
            _rawHeaderFields.RemoveAll(r => r.Name.Span.Equals(headerName, StringComparison.OrdinalIgnoreCase));
        }

        protected abstract int WriteHeaderLine(Span<byte> buffer, bool plainHttp);

        protected abstract int GetHeaderLineLength(bool plainHttp);

        public ReadOnlyMemory<char> GetHttp11Header()
        {
            var estimatedHeaderLength = GetHttp11LengthOnly(true, false, false);

            byte[]? heapBuffer = null;

            try {
                var maxHeader = estimatedHeaderLength < 1024
                    ? stackalloc byte[estimatedHeaderLength]
                    : heapBuffer = ArrayPool<byte>.Shared.Rent(estimatedHeaderLength);

                var totalReadByte = WriteHttp11(false, maxHeader, true, true);

                var res = Encoding.UTF8.GetString(maxHeader.Slice(0, totalReadByte));

                return res.AsMemory();
            }
            finally {
                if (heapBuffer != null) {
                    ArrayPool<byte>.Shared.Return(heapBuffer);
                }
            }
        }

        public override string ToString()
        {
            return GetHttp11Header().ToString();
        }

        public int GetHttp11LengthOnly(bool skipNonForwardableHeader, bool shouldClose, bool plainHttp)
        {
            // Writing Method Path Http Protocol Version
            var totalLength = GetHeaderLineLength(plainHttp);

            foreach (var header in _rawHeaderFields) {
                if (header.Name.Span[0] == ':') // H2 control header
                {
                    continue;
                }

                // HTTP/1.1 forwarding only strips true hop-by-hop headers.
                // Expect: 100-continue is end-to-end on H1 and must be preserved
                // so the origin can answer the client (issue #624).
                if (skipNonForwardableHeader && Http11Constants.IsH1HopByHopHeader(header.Name)) {
                    continue;
                }

                // ASCII: 1 char == 1 byte. Constants ": " (2) and "\r\n" (2) folded in.
                totalLength += header.Name.Length + header.Value.Length + 4;
            }

            totalLength += 2; // final CRLF

            if (shouldClose) {
                totalLength += CloseFlatHeader.Length; // Adding connection close header
            }

            return totalLength;
        }

        public int WriteHttp11(
            bool plainHttp,
            RsBuffer buffer,
            bool skipNonForwardableHeader, bool writeExtraHeaderField = false, bool requestClose = true)
        {
            var totalLength = 0;
            var http11Length = GetHttp11LengthOnly(skipNonForwardableHeader, requestClose, plainHttp);

            while (buffer.Buffer.Length < http11Length) {
                buffer.Extend(http11Length - buffer.Buffer.Length);
            }

            Span<byte> data = buffer.Buffer;

            // Writing Method Path Http Protocol Version
            totalLength += WriteHeaderLine(data, plainHttp);

            foreach (var header in _rawHeaderFields) {
                if (header.Name.Span[0] == ':') // H2 control header
                {
                    continue;
                }

                if (skipNonForwardableHeader && Http11Constants.IsH1HopByHopHeader(header.Name)) {
                    continue;
                }

                totalLength += Encoding.ASCII.GetBytes(header.Name.Span, data.Slice(totalLength));
                ": "u8.CopyTo(data.Slice(totalLength));
                totalLength += 2;
                totalLength += Encoding.ASCII.GetBytes(header.Value.Span, data.Slice(totalLength));
                "\r\n"u8.CopyTo(data.Slice(totalLength));
                totalLength += 2;
            }

            if (requestClose) {
                CloseFlatHeader.AsSpan().CopyTo(data.Slice(totalLength));
                totalLength += CloseFlatHeader.Length;
            }

            "\r\n"u8.CopyTo(data.Slice(totalLength));
            totalLength += 2;

            return totalLength;
        }

        public int WriteHttp11(
            bool plainHttp,
            in Span<byte> data,
            bool skipNonForwardableHeader, bool writeExtraHeaderField = false, bool writeKeepAlive = false)
        {
            var totalLength = 0;

            // Writing Method Path Http Protocol Version
            totalLength += WriteHeaderLine(data, plainHttp);

            foreach (var header in _rawHeaderFields) {
                if (header.Name.Span[0] == ':') // H2 control header
                {
                    continue;
                }

                if (skipNonForwardableHeader && Http11Constants.IsH1HopByHopHeader(header.Name)) {
                    continue;
                }

                totalLength += Encoding.ASCII.GetBytes(header.Name.Span, data.Slice(totalLength));
                ": "u8.CopyTo(data.Slice(totalLength));
                totalLength += 2;
                totalLength += Encoding.ASCII.GetBytes(header.Value.Span, data.Slice(totalLength));
                "\r\n"u8.CopyTo(data.Slice(totalLength));
                totalLength += 2;
            }

            if (writeKeepAlive) {
                KeepAliveFlatHeader.CopyTo(data.Slice(totalLength));
                totalLength += KeepAliveFlatHeader.Length;
            }

            "\r\n"u8.CopyTo(data.Slice(totalLength));
            totalLength += 2;


            return totalLength;
        }

        public int WriteHttp2(
            in Span<byte> data,
            bool skipNonForwardableHeader, bool writeExtraHeaderField = false)
        {
            var totalLength = 0;

            // Writing Method Path Http Protocol Version
            // totalLength += WriteHeaderLine(data);

            foreach (var header in _rawHeaderFields) {
                //if (header.Name.Span[0] == ':') // H2 control header
                //    continue;

                if (skipNonForwardableHeader && Http11Constants.IsNonForwardableHeader(header.Name)) {
                    continue;
                }

                totalLength += Encoding.ASCII.GetBytes(header.Name.Span, data.Slice(totalLength));
                ": "u8.CopyTo(data.Slice(totalLength));
                totalLength += 2;
                totalLength += Encoding.ASCII.GetBytes(header.Value.Span, data.Slice(totalLength));
                "\r\n"u8.CopyTo(data.Slice(totalLength));
                totalLength += 2;
            }

            "\r\n"u8.CopyTo(data.Slice(totalLength));
            totalLength += 2;

            return totalLength;
        }

        protected virtual bool CanHaveBody()
        {
            return true; 
        }

        internal void ForceTransferChunked()
        {
            if (!CanHaveBody()) {
                return;
            }

            if (ChunkedBody) {
                // Upstream response already declared Transfer-Encoding: chunked.
                // Appending a second entry breaks strict HTTP clients (issue #615).
                return;
            }

            _rawHeaderFields.Add(new HeaderField("Transfer-Encoding", "chunked"));
            ChunkedBody = true;
        }
    }
}
