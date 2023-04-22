// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fluxzy.Clients.H2.Encoder;
using Fluxzy.Clients.H2.Encoder.Utils;

namespace Fluxzy.Clients
{
    public class RequestHeader : Header
    {
        /// <summary>
        ///     Building from flat H11
        /// </summary>
        /// <param name="headerContent"></param>
        /// <param name="isSecure"></param>
        public RequestHeader(
            ReadOnlyMemory<char> headerContent,
            bool isSecure)
            : base(headerContent, isSecure)
        {
            Authority = this[Http11Constants.AuthorityVerb].First().Value;
            Path = this[Http11Constants.PathVerb].First().Value;
            Method = this[Http11Constants.MethodVerb].First().Value;
            Scheme = this[Http11Constants.SchemeVerb].First().Value;

            IsWebSocketRequest = this[Http11Constants.ConnectionVerb]
                                     .Any(c => c.Value.Span.Equals("upgrade", StringComparison.OrdinalIgnoreCase))
                                 &&
                                 this[Http11Constants.Upgrade]
                                     .Any(c => c.Value.Span.Equals("websocket", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        ///     Building from explicit headers
        /// </summary>
        /// <param name="headers"></param>
        public RequestHeader(IEnumerable<HeaderField> headers)
            : base(headers)
        {
            Authority = this[Http11Constants.AuthorityVerb].First().Value;
            Path = this[Http11Constants.PathVerb].First().Value;
            Method = this[Http11Constants.MethodVerb].First().Value;
            Scheme = this[Http11Constants.SchemeVerb].First().Value;

            IsWebSocketRequest = this[Http11Constants.ConnectionVerb]
                                     .Any(c => c.Value.Span.Equals("upgrade", StringComparison.OrdinalIgnoreCase))
                                 &&
                                 this[Http11Constants.Upgrade]
                                     .Any(c => c.Value.Span.Equals("websocket", StringComparison.OrdinalIgnoreCase));

           // TODO: Request replay change method 

        }

        public ReadOnlyMemory<char> Authority { get; internal set; }

        public ReadOnlyMemory<char> Path { get; internal set; }

        public ReadOnlyMemory<char> Method { get; internal set; }

        public ReadOnlyMemory<char> Scheme { get; }

        public bool IsWebSocketRequest { get; }

        public string GetFullUrl()
        {
            var stringPath = Path.ToString();

            if (Uri.TryCreate(Path.ToString(), UriKind.Absolute, out var uri) &&
                uri.Scheme.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                return stringPath;

            return $"{Scheme}://{Authority}{stringPath}";
        }

        protected override int WriteHeaderLine(Span<byte> buffer)
        {
            var totalLength = 0;

            totalLength += Encoding.ASCII.GetBytes(Method.Span, buffer.Slice(totalLength));
            totalLength += Encoding.ASCII.GetBytes(" ", buffer.Slice(totalLength));
            totalLength += Encoding.ASCII.GetBytes(Path.Span, buffer.Slice(totalLength));
            totalLength += Encoding.ASCII.GetBytes(" HTTP/1.1\r\n", buffer.Slice(totalLength));
            totalLength += Encoding.ASCII.GetBytes("Host: ", buffer.Slice(totalLength));
            totalLength += Encoding.ASCII.GetBytes(Authority.Span, buffer.Slice(totalLength));
            totalLength += Encoding.ASCII.GetBytes("\r\n", buffer.Slice(totalLength));

            return totalLength;
        }

        protected override int GetHeaderLineLength()
        {
            var totalLength = 0;

            totalLength += Encoding.ASCII.GetByteCount(Method.Span);
            totalLength += Encoding.ASCII.GetByteCount(" ");
            totalLength += Encoding.ASCII.GetByteCount(Path.Span);
            totalLength += Encoding.ASCII.GetByteCount(" HTTP/1.1\r\n");
            totalLength += Encoding.ASCII.GetByteCount("Host: ");
            totalLength += Encoding.ASCII.GetByteCount(Authority.Span);
            totalLength += Encoding.ASCII.GetByteCount("\r\n");

            return totalLength;
        }
    }

    public class ResponseHeader : Header
    {
        /// <summary>
        ///     Building from flat header
        /// </summary>
        /// <param name="headerContent"></param>
        /// <param name="isSecure"></param>
        public ResponseHeader(
            ReadOnlyMemory<char> headerContent,
            bool isSecure)
            : base(headerContent, isSecure)
        {
            StatusCode = int.Parse(this[Http11Constants.StatusVerb].First().Value.Span);

            ConnectionCloseRequest = HeaderFields.Any(
                r => r.Name.Span.Equals(Http11Constants.ConnectionVerb.Span, StringComparison.OrdinalIgnoreCase)
                     && r.Value.Span.Equals("close", StringComparison.OrdinalIgnoreCase));
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
        }

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
