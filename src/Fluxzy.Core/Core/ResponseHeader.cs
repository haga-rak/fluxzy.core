// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fluxzy.Clients.H2.Encoder;
using Fluxzy.Clients.H2.Encoder.Utils;
using Fluxzy.Utils;

namespace Fluxzy.Core
{
    public class ResponseHeader : Header
    {
        private readonly bool _hasCloseDelimitedTransferEncoding;
        private readonly bool _isHttp10Response;

        /// <summary>
        ///     Building from flat header
        /// </summary>
        /// <param name="headerContent"></param>
        /// <param name="isSecure"></param>
        /// <param name="parseConnectionInfo"></param>
        public ResponseHeader(
            ReadOnlyMemory<char> headerContent,
            bool isSecure, bool parseConnectionInfo)
            : base(headerContent, isSecure)
        {
            _isHttp10Response = IsHttp10Response(headerContent.Span);
            _hasCloseDelimitedTransferEncoding = NormalizeCloseDelimitedTransferEncoding();
            StatusCode = ParseStatusCode();

            if (parseConnectionInfo) {
                ConnectionCloseRequest = ReadConnectionCloseRequest();

                if (!ConnectionCloseRequest) {
                    ConnectionCloseRequest = ReadKeepAliveSettings() || ConnectionCloseRequest;
                }
            }
        }

        /// <summary>
        ///     Building from direct header
        /// </summary>
        /// <param name="headers"></param>
        public ResponseHeader(IEnumerable<HeaderField> headers)
            : base(headers)
        {
            _isHttp10Response = false;
            _hasCloseDelimitedTransferEncoding = NormalizeCloseDelimitedTransferEncoding();
            StatusCode = ParseStatusCode();

            ConnectionCloseRequest = ReadConnectionCloseRequest();

            if (!ConnectionCloseRequest) {
                ConnectionCloseRequest = ReadKeepAliveSettings() || ConnectionCloseRequest;
            }
        }

        private int ParseStatusCode()
        {
            if (!TryGetFirstHeader(Http11Constants.StatusVerb, out var field)) {
                throw new InvalidOperationException("Missing ':status' pseudo-header in response.");
            }

            return int.Parse(field.Value.Span);
        }

        private bool NormalizeCloseDelimitedTransferEncoding()
        {
            if (ChunkedBody || !TryGetFirstHeader(Http11Constants.TransferEncodingVerb, out _)) {
                return false;
            }

            // Any Transfer-Encoding overrides Content-Length. A response whose final
            // transfer coding is not chunked is delimited by closing the connection.
            RemoveHeader("content-length");
            ContentLength = -1;

            return true;
        }

        private static bool IsHttp10Response(ReadOnlySpan<char> headerContent)
        {
            var lineStart = 0;

            while (lineStart < headerContent.Length &&
                   (headerContent[lineStart] == '\r' || headerContent[lineStart] == '\n')) {
                lineStart++;
            }

            const string protocol = "HTTP/1.0";
            var firstLine = headerContent.Slice(lineStart);

            return firstLine.Length > protocol.Length &&
                   firstLine.Slice(0, protocol.Length)
                       .Equals(protocol.AsSpan(), StringComparison.OrdinalIgnoreCase) &&
                   (firstLine[protocol.Length] == ' ' || firstLine[protocol.Length] == '\t');
        }

        public int TimeoutIdleSeconds { get; set; } = -1;

        public int MaxConnection { get; set; } = -1;

        public int StatusCode { get; }

        public bool ConnectionCloseRequest { get; }

        private bool ReadConnectionCloseRequest()
        {
            if (HasHeaderValueToken(Http11Constants.ConnectionVerb, "close")) {
                return true;
            }

            if (_hasCloseDelimitedTransferEncoding) {
                return true;
            }

            if (_isHttp10Response &&
                (!HasHeaderValueToken(Http11Constants.ConnectionVerb, "keep-alive") ||
                 !HasSelfDelimitedHttp10Response())) {
                return true;
            }

            // upgrade token only ends the http/1.1 usage when the protocol switch
            // actually happens (101); on any other status it is a mere advertisement
            return StatusCode == 101 &&
                   HasHeaderValueToken(Http11Constants.ConnectionVerb, "upgrade");
        }

        private bool HasSelfDelimitedHttp10Response() =>
            ContentLength >= 0 || StatusCode < 200 || StatusCode is 204 or 304;

        private bool ReadKeepAliveSettings()
        {
            var immediateClose = false;

            if (HasHeaderValueToken(Http11Constants.ConnectionVerb, "keep-alive")) {
                if (TryGetLastHeader(Http11Constants.KeepAliveVerb, out var keepHeaderValue)
                    && !keepHeaderValue.Value.IsEmpty) {
                    if (HeaderUtility.TryParseKeepAlive(keepHeaderValue.Value.Span, out var max, out var timeout)) {
                        if (max >= 0) {
                            MaxConnection = max;

                            if (max == 1) {
                                immediateClose = true;
                            }
                        }

                        if (timeout >= 0) {
                            TimeoutIdleSeconds = timeout;
                        }
                    }
                }
            }

            return immediateClose;
        }

        public bool HasResponseBody(ReadOnlySpan<char> method, out bool shouldClose)
        {
            shouldClose = true;

            if (ContentLength == 0) {
                shouldClose = false;

                return false;
            }

            if (method.Equals("HEAD", StringComparison.OrdinalIgnoreCase)) {
                shouldClose = false;

                return false;
            }

            if (ContentLength > 0) {
                shouldClose = false;

                return true;
            }

            if (StatusCode < 200) {
                return false;
            }

            shouldClose = false;
            return StatusCode != 304 && StatusCode != 204 && StatusCode != 205;
        }

        protected override bool CanHaveBody()
        {
            if (StatusCode == 204 || StatusCode == 205 || StatusCode == 304) {
                return false;
            }

            return true;
        }

        protected override int WriteHeaderLine(Span<byte> buffer, bool _)
        {
            var totalLength = 0;

            // "HTTP/1.1 " = 9 bytes
            "HTTP/1.1 "u8.CopyTo(buffer);
            totalLength += 9;

            if (!Utf8Formatter.TryFormat(StatusCode, buffer.Slice(totalLength), out var written)) {
                throw new InvalidOperationException("Failed to format status code");
            }

            totalLength += written;

            buffer[totalLength++] = (byte) ' ';

            var statusLine = Http11Constants.GetStatusLineBytes(StatusCode);
            statusLine.CopyTo(buffer.Slice(totalLength));
            totalLength += statusLine.Length;

            "\r\n"u8.CopyTo(buffer.Slice(totalLength));
            totalLength += 2;

            return totalLength;
        }

        protected override int GetHeaderLineLength(bool _)
        {
            // "HTTP/1.1 " (9) + <digits> + " " (1) + statusLine + "\r\n" (2)
            return 12 + CountDigits(StatusCode) + Http11Constants.GetStatusLineBytes(StatusCode).Length;
        }

        private static int CountDigits(int value)
        {
            if (value < 0) {
                return CountDigits(-value) + 1;
            }

            if (value < 10) return 1;
            if (value < 100) return 2;
            if (value < 1000) return 3;
            if (value < 10000) return 4;
            if (value < 100000) return 5;
            if (value < 1000000) return 6;
            if (value < 10000000) return 7;
            if (value < 100000000) return 8;
            if (value < 1000000000) return 9;
            return 10;
        }
    }
}
