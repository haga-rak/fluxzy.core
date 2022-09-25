﻿using System;
using System.Linq;

namespace Fluxzy.Utils
{
    public static class HeaderUtility
    {
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

            if (acceptHeader != null) {
                var firstAcceptValue = acceptHeader.Value.ToString().Split(new[] {",", ";"},
                                                       StringSplitOptions.RemoveEmptyEntries)
                                                   .FirstOrDefault();

                if (firstAcceptValue != null)
                    return SolveSimplifiedContentType(firstAcceptValue);
            }

            return null;
        }

        private static string? SolveSimplifiedContentType(ReadOnlySpan<char> headerValue)
        {
            if (headerValue.Contains("json", StringComparison.OrdinalIgnoreCase))
                return "json";

            if (headerValue.Contains("html", StringComparison.OrdinalIgnoreCase))
                return "html";

            if (headerValue.Contains("css", StringComparison.OrdinalIgnoreCase))
                return "css";

            if (headerValue.Contains("xml", StringComparison.OrdinalIgnoreCase))
                return "xml";

            if (headerValue.Contains("javascript", StringComparison.OrdinalIgnoreCase))
                return "javascript";

            if (headerValue.Contains("font", StringComparison.OrdinalIgnoreCase))
                return "font";

            if (headerValue.Contains("image", StringComparison.OrdinalIgnoreCase))
                return "image";

            if (headerValue.Contains("audio", StringComparison.OrdinalIgnoreCase))
                return "audio";

            if (headerValue.Contains("video", StringComparison.OrdinalIgnoreCase))
                return "video";

            if (headerValue.Contains("pdf", StringComparison.OrdinalIgnoreCase))
                return "pdf";

            if (headerValue.Contains("protobuf", StringComparison.OrdinalIgnoreCase))
                return "protobuf";

            if (headerValue.Contains("text/plain", StringComparison.OrdinalIgnoreCase))
                return "text";

            if (headerValue.Contains("xul", StringComparison.OrdinalIgnoreCase))
                return "xul";

            if (headerValue.Contains("zip", StringComparison.OrdinalIgnoreCase))
                return "zip";

            return null;
        }
    }
}