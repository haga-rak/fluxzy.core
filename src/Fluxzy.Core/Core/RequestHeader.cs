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
            InitSettings();
        }

        /// <summary>
        ///     Building from explicit headers
        /// </summary>
        /// <param name="headers"></param>
        public RequestHeader(IEnumerable<HeaderField> headers)
            : base(headers)
        {
            InitSettings();
        }

        private void InitSettings()
        {
            Authority = RequireFirstHeader(Http11Constants.AuthorityVerb).Value;
            Path = RequireFirstHeader(Http11Constants.PathVerb).Value;
            Method = RequireFirstHeader(Http11Constants.MethodVerb).Value;
            Scheme = RequireFirstHeader(Http11Constants.SchemeVerb).Value;

            IsWebSocketRequest = HasHeaderValueContains(Http11Constants.ConnectionVerb, "upgrade")
                                 && HasHeaderValueEqualsAny(Http11Constants.Upgrade, "websocket");

            HasExpectContinue = HasHeaderValueEqualsAny(Http11Constants.Expect, "100-continue");
        }

        private HeaderField RequireFirstHeader(ReadOnlyMemory<char> name)
        {
            if (!TryGetFirstHeader(name, out var field)) {
                throw new InvalidOperationException(
                    $"Missing required pseudo-header '{name}' in request.");
            }

            return field;
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
        public bool IsWebSocketRequest { get; set; }

        /// <summary>
        /// true if the request carries an Expect: 100-continue header.
        /// </summary>
        public bool HasExpectContinue { get; set; }

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
            buffer[totalLength++] = (byte) ' ';

            var path = !plainHttp ? Path.Span : PathAndQueryUtility.Parse(Path.Span);

            totalLength += Encoding.ASCII.GetBytes(path, buffer.Slice(totalLength));

            // " HTTP/1.1\r\nHost: " = 17 bytes
            " HTTP/1.1\r\nHost: "u8.CopyTo(buffer.Slice(totalLength));
            totalLength += 17;

            totalLength += Encoding.ASCII.GetBytes(Authority.Span, buffer.Slice(totalLength));

            "\r\n"u8.CopyTo(buffer.Slice(totalLength));
            totalLength += 2;

            return totalLength;
        }

        protected override int GetHeaderLineLength(bool plainHttp)
        {
            var path = !plainHttp ? Path.Span : PathAndQueryUtility.Parse(Path.Span);

            // Method + " " + path + " HTTP/1.1\r\n" (11) + "Host: " (6) + Authority + "\r\n" (2)
            // = Method.Length + 1 + path.Length + 11 + 6 + Authority.Length + 2
            // = Method.Length + path.Length + Authority.Length + 20
            return Method.Length + path.Length + Authority.Length + 20;
        }
    }
}
