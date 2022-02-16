using System;
using System.Collections.Generic;
using System.Linq;

namespace Echoes.H2.Encoder.Utils
{
    public sealed class Http11Constants
    {
        private static readonly Dictionary<string, string> StatusLineMappingStr = new Dictionary<string, string>()
        {
            { "100", "Continue" },
            { "101", "Switching Protocols" },
            { "102", "Processing" },
            { "200", "OK" },
            { "201", "Created" },
            { "202", "Accepted" },
            { "203", "Non-Authoritative Information" },
            { "204", "No Content" },
            { "205", "Reset Content" },
            { "206", "Partial Content" },
            { "207", "Multi-Status" },
            { "300", "Multiple Choices" },
            { "301", "Moved Permanently" },
            { "302", "Found" },
            { "303", "See Other" },
            { "304", "Not Modified" },
            { "305", "Use Proxy" },
            { "307", "Temporary Redirect" },
            { "308", "Permanent Redirect" },
            { "400", "Bad Request" },
            { "401", "Unauthorized" },
            { "402", "Payment Required" },
            { "403", "Forbidden" },
            { "404", "Not Found" },
            { "405", "Method Not Allowed" },
            { "406", "Not Acceptable" },
            { "407", "Proxy Authentication Required" },
            { "408", "Request Time-out" },
            { "409", "Conflict" },
            { "410", "Gone" },
            { "411", "Length Required" },
            { "412", "Precondition Failed" },
            { "413", "Request Entity Too Large" },
            { "414", "Request-URI Too Large" },
            { "415", "Unsupported Media Type" },
            { "416", "Request Range Not Satisfiable" },
            { "417", "Expectation Failed" },
            { "421", "Misdirected Request" },
            { "422", "Unprocessable Entity" },
            { "423", "Locked" },
            { "424", "Failed Dependency" },
            { "425", "Unordered Collection" },
            { "426", "Upgrade Required" },
            { "428", "Precondition Required" },
            { "429", "Too Many Requests" },
            { "431", "Request Header Fields Too Large" },
            { "451", "Unavailable For Legal Reasons" },
            { "500", "Internal Server Error" },
            { "501", "Not Implemented" },
            { "502", "Bad Gateway" },
            { "503", "Service Unavailable" },
            { "504", "Gateway Time-out" },
            { "505", "HTTP Version Not Supported" },
            { "506", "Variant Also Negotiates" },
            { "507", "Insufficient Storage" },
            { "508", "Loop Detected" },
            { "510", "Not Extended" },
            { "511", "Network Authentication Required" }

        };

        private static readonly Dictionary<ReadOnlyMemory<char>, ReadOnlyMemory<char>> StatusLineMapping =
            StatusLineMappingStr.ToDictionary(t => MemoryExtensions.AsMemory(t.Key), t => MemoryExtensions.AsMemory(t.Value), new SpanCharactersIgnoreCaseComparer());

        public static ReadOnlyMemory<char> GetStatusLine(ReadOnlyMemory<char> statusCode)
        {
            if (StatusLineMapping.TryGetValue(statusCode, out var res))
            {
                return res; 
            }

            return "Unknown status".AsMemory();
        }

        public static readonly HashSet<char> LineSeparators = new HashSet<char>(new[] { '\r', '\n' });
        public static readonly HashSet<char> SpaceSeparators = new HashSet<char>(new[] { ' ', '\t' });
        public static readonly HashSet<char> HeaderSeparator = new HashSet<char>(new[] { ':' });
        public static readonly HashSet<char> CookieSeparators = new HashSet<char>(new[] { ';' });

        public static readonly ReadOnlyMemory<char> MethodVerb = ":method".AsMemory();
        public static readonly ReadOnlyMemory<char> SchemeVerb = ":scheme".AsMemory();
        public static readonly ReadOnlyMemory<char> AuthorityVerb = ":authority".AsMemory();
        public static readonly ReadOnlyMemory<char> PathVerb = ":path".AsMemory();
        public static readonly ReadOnlyMemory<char> StatusVerb = ":status".AsMemory();

        public static readonly ReadOnlyMemory<char> HttpsVerb = "https".AsMemory();
        public static readonly ReadOnlyMemory<char> HttpVerb = "http".AsMemory();
        public static readonly ReadOnlyMemory<char> HostVerb = "host".AsMemory();
        public static readonly ReadOnlyMemory<char> CookieVerb = "cookie".AsMemory();
        public static readonly ReadOnlyMemory<char> ConnectionVerb = "connection".AsMemory();
        public static readonly ReadOnlyMemory<char> UpgradeVerb = "upgrade".AsMemory();
        public static readonly ReadOnlyMemory<char> ContentLength = "content-length".AsMemory();
        public static readonly ReadOnlyMemory<char> TransfertEncodingVerb = "transfer-encoding".AsMemory();
        public static readonly ReadOnlyMemory<char> KeepAliveVerb = "keep-alive".AsMemory();
        public static readonly ReadOnlyMemory<char> ProxyAuthenticate = "proxy-authenticate".AsMemory();
        public static readonly ReadOnlyMemory<char> Trailer = "trailer".AsMemory();
        public static readonly ReadOnlyMemory<char> Upgrade = "upgrade".AsMemory();

        public static readonly HashSet<ReadOnlyMemory<char>> AvoidAutoParseHttp11Headers =
            new HashSet<ReadOnlyMemory<char>>(new[]
            {
                MethodVerb, SchemeVerb, AuthorityVerb, PathVerb, CookieVerb, StatusVerb
            }, new SpanCharactersIgnoreCaseComparer());

        public static readonly HashSet<ReadOnlyMemory<char>> ControlHeaders =
            new HashSet<ReadOnlyMemory<char>>(new[]
            {
                MethodVerb, SchemeVerb, AuthorityVerb, PathVerb, StatusVerb
            }, new SpanCharactersIgnoreCaseComparer());

        public static readonly HashSet<ReadOnlyMemory<char>> NonH2Header =
            new HashSet<ReadOnlyMemory<char>>(new[]
            {
                ConnectionVerb, KeepAliveVerb, ProxyAuthenticate, Trailer, Upgrade
            }, new SpanCharactersIgnoreCaseComparer());


        public static bool IsNonForwardableHeader(ReadOnlyMemory<char> headerName)
        {
            return Http11Constants.NonH2Header.Contains(headerName);
        }
    }
}