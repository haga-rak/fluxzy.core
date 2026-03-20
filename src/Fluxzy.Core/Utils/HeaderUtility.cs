// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Linq;
using Fluxzy.Clients.H2.Encoder.Utils;

namespace Fluxzy.Utils
{
    public static class HeaderUtility
    {
        public static string GetResponseSuggestedExtension(IExchange exchange)
        {
            var contentTypeHeader = exchange
                                    .GetResponseHeaders()
                                    ?.Where(r => r.Name.Span.Equals("content-type", StringComparison.OrdinalIgnoreCase))
                                    .LastOrDefault();

            return ResponseSuggestedExtension(contentTypeHeader);
        }

        internal static string ResponseSuggestedExtension(HeaderFieldInfo? contentTypeHeader)
        {
            if (contentTypeHeader == null)
                return "data";

            if (contentTypeHeader.Value.Span.Contains("json", StringComparison.OrdinalIgnoreCase))
                return "json";

            if (contentTypeHeader.Value.Span.Contains("html", StringComparison.OrdinalIgnoreCase))
                return "html";

            if (contentTypeHeader.Value.Span.Contains("css", StringComparison.OrdinalIgnoreCase))
                return "css";

            if (contentTypeHeader.Value.Span.Contains("xml", StringComparison.OrdinalIgnoreCase))
                return "xml";

            if (contentTypeHeader.Value.Span.Contains("javascript", StringComparison.OrdinalIgnoreCase))
                return "js";

            if (contentTypeHeader.Value.Span.Contains("font", StringComparison.OrdinalIgnoreCase))
                return "font";

            if (contentTypeHeader.Value.Span.Contains("png", StringComparison.OrdinalIgnoreCase))
                return "png";

            if (contentTypeHeader.Value.Span.Contains("jpeg", StringComparison.OrdinalIgnoreCase)
                || contentTypeHeader.Value.Span.Contains("jpg", StringComparison.OrdinalIgnoreCase)
               )
                return "jpeg";

            if (contentTypeHeader.Value.Span.Contains("gif", StringComparison.OrdinalIgnoreCase))
                return "gif";

            if (contentTypeHeader.Value.Span.Contains("svg", StringComparison.OrdinalIgnoreCase))
                return "svg";

            if (contentTypeHeader.Value.Span.Contains("pdf", StringComparison.OrdinalIgnoreCase))
                return "pdf";

            if (contentTypeHeader.Value.Span.Contains("text", StringComparison.OrdinalIgnoreCase))
                return "txt";

            return "data";
        }

        public static string GetRequestSuggestedExtension(IExchange exchange)
        {
            var contentTypeHeader = exchange
                                    .GetRequestHeaders()
                                    ?.Where(r => r.Name.Span.Equals("content-type", StringComparison.OrdinalIgnoreCase))
                                    .LastOrDefault();

            return ResponseSuggestedExtension(contentTypeHeader);
        }

        public static string? GetSimplifiedContentType(IExchange exchange)
        {
            // check into the response Content-Type header first 

            var contentTypeHeader = exchange
                                    .GetResponseHeaders()
                                    ?.Where(r => r.Name.Span.Equals("content-type", StringComparison.OrdinalIgnoreCase))
                                    .LastOrDefault();

            if (contentTypeHeader != null) {
                var target = contentTypeHeader.Value.Span;

                var result = SolveSimplifiedContentType(target);

                if (result != null)
                    return result;

                return target.ToString();
            }

            // check into request Accept  header

            var acceptHeader = exchange
                               .GetResponseHeaders()
                               ?.Where(r => r.Name.Span.Equals("accept", StringComparison.OrdinalIgnoreCase))
                               .LastOrDefault();

            var firstAcceptValue = acceptHeader?.Value.ToString().Split(new[] {",", ";"},
                                                   StringSplitOptions.RemoveEmptyEntries)
                                               .FirstOrDefault();

            if (firstAcceptValue != null)
                return SolveSimplifiedContentType(firstAcceptValue);

            return null;
        }

        private static string? SolveSimplifiedContentType(ReadOnlySpan<char> headerValue)
        {
            // gRPC (check before protobuf since grpc+proto is common)
            if (headerValue.Contains("grpc", StringComparison.OrdinalIgnoreCase))
                return "grpc";

            // Structured data
            if (headerValue.Contains("json", StringComparison.OrdinalIgnoreCase))
                return "json";
            if (headerValue.Contains("protobuf", StringComparison.OrdinalIgnoreCase))
                return "pbuf";
            if (headerValue.Contains("yaml", StringComparison.OrdinalIgnoreCase))
                return "yaml";
            if (headerValue.Contains("csv", StringComparison.OrdinalIgnoreCase))
                return "csv";
            if (headerValue.Contains("msgpack", StringComparison.OrdinalIgnoreCase))
                return "msgpack";
            if (headerValue.Contains("cbor", StringComparison.OrdinalIgnoreCase))
                return "cbor";
            if (headerValue.Contains("graphql", StringComparison.OrdinalIgnoreCase))
                return "graphql";

            // Web content
            if (headerValue.Contains("html", StringComparison.OrdinalIgnoreCase))
                return "html";
            if (headerValue.Contains("javascript", StringComparison.OrdinalIgnoreCase))
                return "js";
            if (headerValue.Contains("css", StringComparison.OrdinalIgnoreCase))
                return "css";
            if (headerValue.Contains("wasm", StringComparison.OrdinalIgnoreCase))
                return "wasm";
            if (headerValue.Contains("event-stream", StringComparison.OrdinalIgnoreCase))
                return "sse";

            // Images (svg before generic image, and before xml)
            if (headerValue.Contains("svg", StringComparison.OrdinalIgnoreCase))
                return "img";
            if (headerValue.Contains("image", StringComparison.OrdinalIgnoreCase))
                return "img";

            // XML (after svg+xml and html checks)
            if (headerValue.Contains("xml", StringComparison.OrdinalIgnoreCase))
                return "xml";

            // Media
            if (headerValue.Contains("audio", StringComparison.OrdinalIgnoreCase))
                return "audio";
            if (headerValue.Contains("video", StringComparison.OrdinalIgnoreCase))
                return "video";
            if (headerValue.Contains("mpegurl", StringComparison.OrdinalIgnoreCase))
                return "hls";

            // Fonts
            if (headerValue.Contains("font", StringComparison.OrdinalIgnoreCase))
                return "font";

            // Forms
            if (headerValue.Contains("multipart", StringComparison.OrdinalIgnoreCase))
                return "multipart";
            if (headerValue.Contains("form-urlencoded", StringComparison.OrdinalIgnoreCase))
                return "form";

            // Documents
            if (headerValue.Contains("pdf", StringComparison.OrdinalIgnoreCase))
                return "pdf";
            if (headerValue.Contains("wordprocessingml", StringComparison.OrdinalIgnoreCase)
                || headerValue.Contains("msword", StringComparison.OrdinalIgnoreCase))
                return "doc";
            if (headerValue.Contains("spreadsheetml", StringComparison.OrdinalIgnoreCase)
                || headerValue.Contains("ms-excel", StringComparison.OrdinalIgnoreCase))
                return "xls";
            if (headerValue.Contains("presentationml", StringComparison.OrdinalIgnoreCase)
                || headerValue.Contains("ms-powerpoint", StringComparison.OrdinalIgnoreCase))
                return "ppt";

            // Text
            if (headerValue.Contains("text/plain", StringComparison.OrdinalIgnoreCase))
                return "text";

            // Archives
            if (headerValue.Contains("zip", StringComparison.OrdinalIgnoreCase))
                return "zip";
            if (headerValue.Contains("gzip", StringComparison.OrdinalIgnoreCase))
                return "gz";
            if (headerValue.Contains("tar", StringComparison.OrdinalIgnoreCase))
                return "tar";

            // DNS over HTTPS
            if (headerValue.Contains("dns-message", StringComparison.OrdinalIgnoreCase))
                return "dns";

            // Binary fallback
            if (headerValue.Contains("octet", StringComparison.OrdinalIgnoreCase))
                return "bin";

            // Legacy
            if (headerValue.Contains("xul", StringComparison.OrdinalIgnoreCase))
                return "xul";

            return null;
        }
        public static bool TryParseKeepAlive(ReadOnlySpan<char> keepAliveValue, out int max, out int timeout)
        {
            max = -1;
            timeout = -1;

            var enumerator = keepAliveValue.Split(",");

            while (enumerator.MoveNext()) {
                var value = enumerator.Current.Trim();

                if (max < 0 && value.StartsWith("max=", StringComparison.OrdinalIgnoreCase)) {
                    if (int.TryParse(value[4..], out var maxResult)) {
                        max = maxResult;
                        continue;
                    }
                }

                if (timeout < 0 && value.StartsWith("timeout=", StringComparison.OrdinalIgnoreCase)) {
                    if (int.TryParse(value[8..], out var timeoutResult)) {
                        timeout = timeoutResult;
                        continue;
                    }
                }

                if (max > 0 && timeout > 0)
                    break;
            }

            return max > 0 || timeout > 0;
        
        }
    }
}
