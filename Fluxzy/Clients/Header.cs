// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fluxzy.Clients.H2.Encoder;
using Fluxzy.Clients.H2.Encoder.Utils;
using Fluxzy.Misc.ResizableBuffers;

namespace Fluxzy.Clients
{
    public abstract class Header
    {
        private readonly ILookup<ReadOnlyMemory<char>, HeaderField> _lookupFields;
        private readonly List<HeaderField> _rawHeaderFields;

        protected Header(IEnumerable<HeaderField> headerFields)
        {
            _rawHeaderFields = headerFields.ToList(); 

            _lookupFields = _rawHeaderFields
                .ToLookup(t => t.Name, t => t, SpanCharactersIgnoreCaseComparer.Default);

            ChunkedBody = _lookupFields[Http11Constants.TransferEncodingVerb]
                .Any(t => t.Value.Span.Equals("chunked", StringComparison.OrdinalIgnoreCase));

            var contentLength = -1L;

            // In case of multiple content length we cake the last 
            if (_lookupFields[Http11Constants.ContentLength].Any(t => long.TryParse(t.Value.Span, out contentLength)))
                ContentLength = contentLength;
        }

        protected Header(
            ReadOnlyMemory<char> rawHeader,
            bool isSecure)
            : this (Http11Parser.Read(rawHeader, isSecure, true, false))
        {

        }
        

        public IEnumerable<HeaderField> this[ReadOnlyMemory<char> key] => _lookupFields[key];

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
                    if (Http11Constants.IsNonForwardableHeader(header.Name))
                        continue;

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

        public void AltReplaceHeaders(string name, string value)
        {
            var replaceHeaders = _rawHeaderFields.Where(r => r.Name.Span.Equals(name,
                StringComparison.OrdinalIgnoreCase)).ToList();

            _rawHeaderFields.RemoveAll(r => r.Name.Span.Equals(name,
                StringComparison.OrdinalIgnoreCase));

            foreach (var replaceHeader in replaceHeaders) {
                var previousValue = replaceHeader.Value;
                var finalValue = value.Replace("{{previous}}", previousValue.ToString());

                var replacement = new HeaderField(
                    replaceHeader.Name,
                    finalValue.AsMemory()
                );

                _rawHeaderFields.Add(replacement);
            }
        }

        protected abstract int WriteHeaderLine(Span<byte> buffer);

        protected abstract int GetHeaderLineLength();

        public ReadOnlyMemory<char> GetHttp11Header()
        {
            var estimatedHeaderLength = GetHttp11LengthOnly(true);

            byte[]? heapBuffer = null;

            try {
                var maxHeader = estimatedHeaderLength < 1024
                    ? stackalloc byte[estimatedHeaderLength]
                    : heapBuffer = ArrayPool<byte>.Shared.Rent(estimatedHeaderLength);

                var totalReadByte = WriteHttp11(maxHeader, true, true);

                var res = Encoding.UTF8.GetString(maxHeader.Slice(0, totalReadByte));

                return res.AsMemory();
            }
            finally {
                if (heapBuffer != null)
                    ArrayPool<byte>.Shared.Return(heapBuffer);
            }
        }

        public override string ToString()
        {
            return GetHttp11Header().ToString();
        }

        public int GetHttp11LengthOnly(bool skipNonForwardableHeader)
        {
            var totalLength = 0;

            // Writing Method Path Http Protocol Version
            totalLength += GetHeaderLineLength();

            foreach (var header in _rawHeaderFields) {
                if (header.Name.Span[0] == ':') // H2 control header 
                    continue;

                if (skipNonForwardableHeader && Http11Constants.IsNonForwardableHeader(header.Name))
                    continue;

                totalLength += Encoding.ASCII.GetByteCount(header.Name.Span);
                totalLength += Encoding.ASCII.GetByteCount(": ");
                totalLength += Encoding.ASCII.GetByteCount(header.Value.Span);
                totalLength += Encoding.ASCII.GetByteCount("\r\n");
            }

            totalLength += Encoding.ASCII.GetByteCount("\r\n");

            return totalLength;
        }

        public int WriteHttp11(
            RsBuffer buffer,
            bool skipNonForwardableHeader, bool writeExtraHeaderField = false)
        {
            var totalLength = 0;
            var http11Length = GetHttp11LengthOnly(skipNonForwardableHeader);

            while (buffer.Buffer.Length < http11Length) {
                buffer.Extend(2);
            }

            Span<byte> data = buffer.Buffer;

            // Writing Method Path Http Protocol Version
            totalLength += WriteHeaderLine(data);

            foreach (var header in _rawHeaderFields) {
                if (header.Name.Span[0] == ':') // H2 control header 
                    continue;

                if (skipNonForwardableHeader && Http11Constants.IsNonForwardableHeader(header.Name))
                    continue;

                totalLength += Encoding.ASCII.GetBytes(header.Name.Span, data.Slice(totalLength));
                totalLength += Encoding.ASCII.GetBytes(": ", data.Slice(totalLength));
                totalLength += Encoding.ASCII.GetBytes(header.Value.Span, data.Slice(totalLength));
                totalLength += Encoding.ASCII.GetBytes("\r\n", data.Slice(totalLength));
            }

            totalLength += Encoding.ASCII.GetBytes("\r\n", data.Slice(totalLength));

            return totalLength;
        }

        public int WriteHttp11(
            in Span<byte> data,
            bool skipNonForwardableHeader, bool writeExtraHeaderField = false)
        {
            var totalLength = 0;

            // Writing Method Path Http Protocol Version
            totalLength += WriteHeaderLine(data);

            foreach (var header in _rawHeaderFields) {
                if (header.Name.Span[0] == ':') // H2 control header 
                    continue;

                if (skipNonForwardableHeader && Http11Constants.IsNonForwardableHeader(header.Name))
                    continue;

                totalLength += Encoding.ASCII.GetBytes(header.Name.Span, data.Slice(totalLength));
                totalLength += Encoding.ASCII.GetBytes(": ", data.Slice(totalLength));
                totalLength += Encoding.ASCII.GetBytes(header.Value.Span, data.Slice(totalLength));
                totalLength += Encoding.ASCII.GetBytes("\r\n", data.Slice(totalLength));
            }

            totalLength += Encoding.ASCII.GetBytes("\r\n", data.Slice(totalLength));

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

                if (skipNonForwardableHeader && Http11Constants.IsNonForwardableHeader(header.Name))
                    continue;

                totalLength += Encoding.ASCII.GetBytes(header.Name.Span, data.Slice(totalLength));
                totalLength += Encoding.ASCII.GetBytes(": ", data.Slice(totalLength));
                totalLength += Encoding.ASCII.GetBytes(header.Value.Span, data.Slice(totalLength));
                totalLength += Encoding.ASCII.GetBytes("\r\n", data.Slice(totalLength));
            }

            totalLength += Encoding.ASCII.GetBytes("\r\n", data.Slice(totalLength));

            return totalLength;
        }

        internal void ForceTransferChunked()
        {
            _rawHeaderFields.Add(new HeaderField("Transfer-Encoding", "chunked"));
            ChunkedBody = true;
        }
    }
}
