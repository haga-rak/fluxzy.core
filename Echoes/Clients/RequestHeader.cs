// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Echoes.H2.Encoder;
using Echoes.H2.Encoder.Utils;

namespace Echoes
{
    public class RequestHeader : Header
    {
        public RequestHeader(
            ReadOnlyMemory<char> headerContent,
            bool isSecure,
            Http11Parser parser)
            : base(headerContent, isSecure, parser)
        {
            Authority = this[Http11Constants.AuthorityVerb].First().Value;
            Path = this[Http11Constants.PathVerb].First().Value;
            Method = this[Http11Constants.MethodVerb].First().Value;
            IsWebSocketRequest = this[Http11Constants.ConnectionVerb]
                .Any(c => c.Value.Span.Equals("upgrade", StringComparison.OrdinalIgnoreCase)) 
                && 
                this[Http11Constants.Upgrade]
                .Any(c => c.Value.Span.Equals("websocket", StringComparison.OrdinalIgnoreCase)); 
        }

        public ReadOnlyMemory<char> Authority { get;  }

        public ReadOnlyMemory<char> Path { get;  }

        public ReadOnlyMemory<char> Method { get;  }

        public bool IsWebSocketRequest { get;  }

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
    }

    public class ResponseHeader : Header
    {
        public ResponseHeader(
            ReadOnlyMemory<char> headerContent,
            bool isSecure,
            Http11Parser parser)
            : base(headerContent, isSecure, parser)
        {
            StatusCode = int.Parse(this[Http11Constants.StatusVerb].First().Value.Span);
            ConnectionCloseRequest = HeaderFields.Any(
                r => r.Name.Span.Equals(Http11Constants.ConnectionVerb.Span, StringComparison.OrdinalIgnoreCase)
                     && r.Value.Span.Equals("close", StringComparison.OrdinalIgnoreCase)); 
        }

        public int StatusCode { get;  }

        public bool ConnectionCloseRequest { get;  }

        public bool HasResponseBody()
        {
            if (ContentLength > 0)
                return true;

            return  StatusCode != 304 && StatusCode >= 200 && StatusCode != 204 && StatusCode != 205;
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
    }

    public abstract class Header
    {
        public ReadOnlyMemory<char> RawHeader { get; }

        protected List<HeaderField> _rawHeaderFields;

        protected ILookup<ReadOnlyMemory<char>, HeaderField> _lookupFields ;

        protected Header(
            ReadOnlyMemory<char> rawHeader, 
            bool isSecure, Http11Parser parser)
        {
            RawHeader = rawHeader;
            HeaderLength = rawHeader.Length; 

            _rawHeaderFields = parser.Read(rawHeader, isSecure, true, false).ToList();

            _lookupFields = _rawHeaderFields
                .ToLookup(t => t.Name, t => t, SpanCharactersIgnoreCaseComparer.Default);

            ChunkedBody = _lookupFields[Http11Constants.TransfertEncodingVerb]
                .Any(t => t.Value.Span.Equals("chunked", StringComparison.OrdinalIgnoreCase));

            var contentLength = -1L; 

            // In case of multiple content length we cake the last 
            if (_lookupFields[Http11Constants.ContentLength].Any(t => long.TryParse(t.Value.Span, out contentLength)))
            {
                ContentLength = contentLength;
            }
        }

        public int HeaderLength { get; }

        public IEnumerable<HeaderField> this[ReadOnlyMemory<char> key] => _lookupFields[key];

        /// <summary>
        /// If transfer-encoding chunked is defined 
        /// </summary>
        public bool ChunkedBody { get; private set; }

        /// <summary>
        /// Content length of body. -1 if undefined 
        /// </summary>
        public long ContentLength { get; set; } = -1;

        public IReadOnlyCollection<HeaderField> HeaderFields => _rawHeaderFields;

        protected abstract int WriteHeaderLine(Span<byte> buffer);

        public override string ToString()
        {
            return RawHeader.ToString();
        }

        public int WriteHttp11(in Span<byte> data, bool skipNonForwardableHeader)
        {
            var totalLength = 0;
           
            totalLength += WriteHeaderLine(data);


            foreach (var header in _rawHeaderFields)
            {
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

        internal void ForceTransferChunked()
        {
            _rawHeaderFields.Add(new HeaderField("Transfer-Encoding", "chunked"));
            ChunkedBody = true; 
        }
    }
}