// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fluxzy.Clients.H2.Encoder;
using Fluxzy.Clients.H2.Encoder.Utils;
using Fluxzy.Misc;

namespace Fluxzy.Core
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
            Authority = this[Http11Constants.AuthorityVerb].FirstOrDefault().Value;
            Path = this[Http11Constants.PathVerb].FirstOrDefault().Value;
            Method = this[Http11Constants.MethodVerb].FirstOrDefault().Value;
            Scheme = this[Http11Constants.SchemeVerb].FirstOrDefault().Value;

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

        /// <summary>
        /// Authority, can contain port number prefixed with ':'
        /// </summary>
        public ReadOnlyMemory<char> Authority { get; internal set; }

        /// <summary>
        /// Request PATH
        /// </summary>
        public ReadOnlyMemory<char> Path { get; set; }

        /// <summary>
        /// Request method
        /// </summary>
        public ReadOnlyMemory<char> Method { get; set; }

        /// <summary>
        /// Request scheme
        /// </summary>
        public ReadOnlyMemory<char> Scheme { get; set; }

        /// <summary>
        /// true if it's a websocket request
        /// </summary>
        public bool IsWebSocketRequest { get; }

        /// <summary>
        /// Full URL building with Authority, path and scheme
        /// </summary>
        /// <returns></returns>
        public string GetFullUrl()
        {
            var stringPath = Path.ToString();

            if (Uri.TryCreate(Path.ToString(), UriKind.Absolute, out var uri) &&
                uri.Scheme.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                return stringPath;

            return $"{Scheme}://{Authority}{stringPath}";
        }

        protected override int WriteHeaderLine(Span<byte> buffer, bool plainHttp)
        {
            var totalLength = 0;

            totalLength += Encoding.ASCII.GetBytes(Method.Span, buffer.Slice(totalLength));
            totalLength += Encoding.ASCII.GetBytes(" ", buffer.Slice(totalLength));

            var path = !plainHttp ? Path.Span : PathAndQueryUtility.Parse(Path.Span);

            totalLength += Encoding.ASCII.GetBytes(path, buffer.Slice(totalLength));

            totalLength += Encoding.ASCII.GetBytes(" HTTP/1.1\r\n", buffer.Slice(totalLength));
            totalLength += Encoding.ASCII.GetBytes("Host: ", buffer.Slice(totalLength));
            totalLength += Encoding.ASCII.GetBytes(Authority.Span, buffer.Slice(totalLength));
            totalLength += Encoding.ASCII.GetBytes("\r\n", buffer.Slice(totalLength));

            return totalLength;
        }

        protected override int GetHeaderLineLength(bool plainHttp)
        {
            var totalLength = 0;

            totalLength += Encoding.ASCII.GetByteCount(Method.Span);
            totalLength += Encoding.ASCII.GetByteCount(" ");

            var path = !plainHttp ? Path.Span : PathAndQueryUtility.Parse(Path.Span);

            totalLength += Encoding.ASCII.GetByteCount(path);

            totalLength += Encoding.ASCII.GetByteCount(" HTTP/1.1\r\n");
            totalLength += Encoding.ASCII.GetByteCount("Host: ");
            totalLength += Encoding.ASCII.GetByteCount(Authority.Span);
            totalLength += Encoding.ASCII.GetByteCount("\r\n");

            return totalLength;
        }
    }
}
