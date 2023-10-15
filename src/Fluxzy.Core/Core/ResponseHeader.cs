// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
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
            StatusCode = int.Parse(this[Http11Constants.StatusVerb].First().Value.Span);

            if (parseConnectionInfo) {
                ConnectionCloseRequest = HeaderFields.Any(
                    r => r.Name.Span.Equals(Http11Constants.ConnectionVerb.Span, StringComparison.OrdinalIgnoreCase)
                         && r.Value.Span.Equals("close", StringComparison.OrdinalIgnoreCase));

                if (!ConnectionCloseRequest)
                    ConnectionCloseRequest = ReadKeepAliveSettings() || ConnectionCloseRequest;
            }
        }

        /// <summary>
        ///     Building from direct header
        /// </summary>
        /// <param name="headers"></param>
        public ResponseHeader(IEnumerable<HeaderField> headers)
            : base(headers)
        {
            StatusCode = int.Parse(this[Http11Constants.StatusVerb].First().Value.Span);

            ConnectionCloseRequest = HeaderFields.Any(
                r => r.Name.Span.Equals(Http11Constants.ConnectionVerb.Span, StringComparison.OrdinalIgnoreCase)
                     && r.Value.Span.Equals("close", StringComparison.OrdinalIgnoreCase));

            if (!ConnectionCloseRequest)
                ConnectionCloseRequest = ReadKeepAliveSettings() || ConnectionCloseRequest;
        }

        private bool ReadKeepAliveSettings()
        {
            var immediateClose = false;

            if (HeaderFields.Any(
                    r => r.Name.Span.Equals(Http11Constants.ConnectionVerb.Span, StringComparison.OrdinalIgnoreCase)
                         && r.Value.Span.Equals("keep-alive", StringComparison.OrdinalIgnoreCase)))
            {
                var keepHeaderValue = HeaderFields.LastOrDefault(
                    h => h.Name.Span.Equals(Http11Constants.KeepAliveVerb.Span, StringComparison.OrdinalIgnoreCase)
                );

                if (!keepHeaderValue.Value.IsEmpty)
                {
                    if (HeaderUtility.TryParseKeepAlive(keepHeaderValue.Value.Span, out var max, out var timeout))
                    {
                        if (max >= 0)
                        {
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

        public int TimeoutIdleSeconds { get; set; } = 1;

        public int MaxConnection { get; set; } = -1;

        public int StatusCode { get; }

        public bool ConnectionCloseRequest { get; }

        public bool HasResponseBody()
        {
            if (ContentLength == 0)
                return false;

            if (ContentLength > 0)
                return true;

            return StatusCode != 304 && StatusCode >= 200 && StatusCode != 204 && StatusCode != 205;
        }

        protected override int WriteHeaderLine(Span<byte> buffer)
        {
            var totalLength = 0;

            var statusCodeString = StatusCode.ToString();

            totalLength += Encoding.ASCII.GetBytes("HTTP/1.1 ", buffer.Slice(totalLength));
            totalLength += Encoding.ASCII.GetBytes(statusCodeString, buffer.Slice(totalLength));
            totalLength += Encoding.ASCII.GetBytes(" ", buffer.Slice(totalLength));

            totalLength += Encoding.ASCII.GetBytes(Http11Constants.GetStatusLine(statusCodeString.AsMemory()).Span,
                buffer.Slice(totalLength));

            totalLength += Encoding.ASCII.GetBytes("\r\n", buffer.Slice(totalLength));

            return totalLength;
        }

        protected override int GetHeaderLineLength()
        {
            var totalLength = 0;

            var statusCodeString = StatusCode.ToString();

            totalLength += Encoding.ASCII.GetByteCount("HTTP/1.1 ");
            totalLength += Encoding.ASCII.GetByteCount(statusCodeString);
            totalLength += Encoding.ASCII.GetByteCount(" ");
            totalLength += Encoding.ASCII.GetByteCount(Http11Constants.GetStatusLine(statusCodeString.AsMemory()).Span);
            totalLength += Encoding.ASCII.GetByteCount("\r\n");

            return totalLength;
        }
    }
}
