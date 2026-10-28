using System;
using System.Net;
using System.Text;
using Fluxzy.Core;

namespace Fluxzy.Utils
{
    public static class AuthorityUtility
    {
        /// <summary>
        /// Resolve the target authority of a plain (non-tunneled) request, from an
        /// absolute-form URI or the Host header.
        /// </summary>
        /// <summary>
        /// True when the request target is absolute-form with a scheme Fluxzy cannot
        /// forward (ftp://, ws://...). Happens when a system proxy registration covers
        /// every protocol and a client sends an FTP-over-HTTP gateway request (#821).
        /// </summary>
        internal static bool TryGetUnsupportedScheme(RequestHeader header, out Uri uri)
        {
            uri = null!;

            var path = header.Path.Span;

            if (path.IndexOf("://".AsSpan(), StringComparison.Ordinal) <= 0)
                return false;

            if (!Uri.TryCreate(path.ToString(), UriKind.Absolute, out var parsed))
                return false;

            if (parsed.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase)
                || parsed.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
                return false;

            uri = parsed;

            return true;
        }

        internal static bool TryParsePlainRequestAuthority(
            RequestHeader header, int? forcedPort, out Authority authority)
        {
            authority = default;

            var path = header.Path.ToString();

            if (!Uri.TryCreate(path, UriKind.Absolute, out var uri)
                || !uri.Scheme.StartsWith("http", StringComparison.OrdinalIgnoreCase)) {
                var builder = new StringBuilder("http://");

                builder.Append(header.Authority.Span);

                if (!path.StartsWith("/"))
                    builder.Append("/");

                builder.Append(path);

                if (!Uri.TryCreate(builder.ToString(), UriKind.Absolute, out uri))
                    return false;
            }

            authority = new Authority(uri.Host, forcedPort ?? uri.Port, false);

            return true;
        }

        /// <summary>
        /// Parse an authority, accepted separator are ':' and '/'.
        /// </summary>
        /// <param name="rawValue"></param>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public static bool TryParse(string rawValue, out string? host, out int port)
        {
            host = null; 
            port = 0;

            var separators = new[] { ":", "/"};

            foreach (var separator in separators) {
                var lastColumn = rawValue.LastIndexOf(separator, StringComparison.Ordinal);

                if (lastColumn == -1)
                    continue;

                var hostName = rawValue.Substring(0, lastColumn).TrimStart();
                var rawPort = rawValue.Substring(lastColumn + 1).TrimEnd();

                if (hostName == string.Empty) {
                    continue; 
                }

                if (!int.TryParse(rawPort, out port))
                    continue;

                if (port < 0 || port > 65535) {
                    port = 0;
                    return false; 
                }

                host = hostName;

                return true;
            }

            return false;
        }

        /// <summary>
        ///  Parse an authority, accepted separator are ':' and '/'. Authority must me a valid IPv4 or IPv6 address.
        /// </summary>
        /// <param name="rawValue"></param>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public static bool TryParseIp(string rawValue, out IPAddress? ip, out int port)
        {
            ip = null; 
            port = 0;

            var result = TryParse(rawValue, out var rawHost, out port);

            if (!result)
                return false;

            if (IPAddress.TryParse(rawHost!, out var parsedIp)) {
                ip = parsedIp;
                return true;
            }

            ip = null;
            port = 0;

            return false;
        }
    }
}