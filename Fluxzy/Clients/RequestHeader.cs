// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.IO;
using System.Linq;
using System.Text;
using Fluxzy.Clients.H2.Encoder.Utils;

namespace Fluxzy.Clients
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
            Scheme = this[Http11Constants.SchemeVerb].First().Value;
            IsWebSocketRequest = this[Http11Constants.ConnectionVerb]
                .Any(c => c.Value.Span.Equals("upgrade", StringComparison.OrdinalIgnoreCase)) 
                && 
                this[Http11Constants.Upgrade]
                .Any(c => c.Value.Span.Equals("websocket", StringComparison.OrdinalIgnoreCase)); 
        }

        public ReadOnlyMemory<char> Authority { get;  }

        public ReadOnlyMemory<char> Path { get;  }

        public ReadOnlyMemory<char> Method { get;  }

        public ReadOnlyMemory<char> Scheme { get;  }

        public bool IsWebSocketRequest { get;  }

        public string GetFullUrl()
        {
            return $"{Scheme}://{Authority}{Path}"; 
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
            if (ContentLength == 0)
                return false;

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
}