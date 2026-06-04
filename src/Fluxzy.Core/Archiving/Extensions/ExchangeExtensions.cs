// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Fluxzy.Extensions
{
    public static class ExchangeExtensions
    {
        public static IEnumerable<HeaderFieldInfo> Find(this IEnumerable<HeaderFieldInfo> headers, string headerName)
        {
            return headers.Where(h =>
                h.Name.Span.Equals(headerName, StringComparison.OrdinalIgnoreCase));
        }

        public static string? GetRequestHeaderValue(this IExchange exchangeInfo, string headerName)
        {
            var contentTypeHeader = exchangeInfo.GetRequestHeaders().LastOrDefault(h =>
                h.Name.Span.Equals(headerName, StringComparison.OrdinalIgnoreCase));

            return contentTypeHeader?.Value.ToString();
        }

        public static string? GetResponseHeaderValue(this IExchange exchangeInfo, string headerName)
        {
            var headers = exchangeInfo.GetResponseHeaders();

            if (headers == null)
                return null;

            var contentTypeHeader = headers.LastOrDefault(h =>
                h.Name.Span.Equals(headerName, StringComparison.OrdinalIgnoreCase));

            return contentTypeHeader?.Value.ToString();
        }

        public static Encoding? GetResponseEncoding(this IExchange exchangeInfo)
        {
            var headers = exchangeInfo.GetResponseHeaders();

            if (headers == null)
                return null;

            var contentTypeHeader = headers.LastOrDefault(h =>
                h.Name.Span.Equals("Content-Type", StringComparison.OrdinalIgnoreCase));

            if (contentTypeHeader == null)
                return null;

            var valueString = contentTypeHeader.Value.ToString();
            var matchResult = Regex.Match(valueString, @"charset=([a-zA-Z\-0-9]+)");

            if (matchResult.Success) {
                try {
                    return Encoding.GetEncoding(matchResult.Groups[1].Value);
                }
                catch (ArgumentException) {
                }
            }

            return null;
        }

        public static bool IsResponseChunkedTransferEncoded(this IExchange exchangeInfo, bool skipForwarded = false)
        {
            var headers = exchangeInfo.GetResponseHeaders();
            return IsChunkedTransferEncoded(skipForwarded, headers);
        }

        private static bool IsChunkedTransferEncoded(bool skipForwarded, IEnumerable<HeaderFieldInfo>? headers)
        {
            if (headers == null)
                return false;

            return headers.Any(h =>
                (skipForwarded || !h.Forwarded) && // ----> What is this condition
                h.Name.Span.Equals("Transfer-Encoding",
                    StringComparison.OrdinalIgnoreCase)
                && h.Value.Span.Equals("chunked",
                    StringComparison.OrdinalIgnoreCase)
            );
        }

        public static bool IsRequestChunkedTransferEncoded(this IExchange exchangeInfo, bool skipForwarded = false)
        {
            var headers = exchangeInfo.GetRequestHeaders();
            return IsChunkedTransferEncoded(skipForwarded, headers);
        }

        public static CompressionType GetResponseCompressionType(this IExchange exchangeInfo)
        {
            return TokenToCompressionType(exchangeInfo.GetResponseContentEncoding());
        }

        public static CompressionType GetRequestCompressionType(this IExchange exchangeInfo)
        {
            var headers = exchangeInfo.GetRequestHeaders();

            if (headers == null)
                throw new InvalidOperationException("This exchange does not have request body");

            return TokenToCompressionType(InternalGetEncodingToken(headers));
        }

        /// <summary>
        ///     Returns the raw response content-encoding token (lowercased, e.g. "gzip", "br", "compress", "zstd"),
        ///     or null when the response is not encoded. Unlike <see cref="GetResponseCompressionType" /> this also
        ///     surfaces encodings that are not represented by <see cref="CompressionType" /> (e.g. "zstd"), which are
        ///     resolved through <see cref="ContentDecoderRegistry" />.
        /// </summary>
        public static string? GetResponseContentEncoding(this IExchange exchangeInfo)
        {
            var headers = exchangeInfo.GetResponseHeaders();

            return headers == null ? null : InternalGetEncodingToken(headers);
        }

        /// <summary>
        ///     Returns the raw request content-encoding token (lowercased), or null when the request is not encoded.
        /// </summary>
        public static string? GetRequestContentEncoding(this IExchange exchangeInfo)
        {
            var headers = exchangeInfo.GetRequestHeaders();

            return headers == null ? null : InternalGetEncodingToken(headers);
        }

        internal static CompressionType TokenToCompressionType(string? encodingToken)
        {
            if (string.IsNullOrEmpty(encodingToken))
                return CompressionType.None;

            switch (encodingToken) {
                case "gzip":
                    return CompressionType.Gzip;
                case "deflate":
                    return CompressionType.Deflate;
                case "br":
                case "brotli":
                    return CompressionType.Brotli;
                case "compress":
                    return CompressionType.Compress;
                default:
                    // Registry-handled encodings (e.g. "zstd") are not represented by the enum; callers
                    // that need to act on them should use the raw token via GetResponse/RequestContentEncoding.
                    return CompressionType.None;
            }
        }

        private static string? InternalGetEncodingToken(IEnumerable<HeaderFieldInfo> headers)
        {
            var encodingHeaders = headers.Where(h => (h.Forwarded &&
                                                      h.Name.Span.Equals("Transfer-Encoding",
                                                          StringComparison.OrdinalIgnoreCase))
                                                     || h.Name.Span.Equals("Content-encoding",
                                                         StringComparison.OrdinalIgnoreCase)).ToList();

            if (encodingHeaders.Count == 0)
                return null;

            if (encodingHeaders.Any(h => h.Value.Span.Contains("gzip", StringComparison.OrdinalIgnoreCase)))
                return "gzip";

            if (encodingHeaders.Any(h => h.Value.Span.Contains("deflate", StringComparison.OrdinalIgnoreCase)))
                return "deflate";

            if (encodingHeaders.Any(h => h.Value.Span.Contains("br", StringComparison.OrdinalIgnoreCase)))
                return "br";

            if (encodingHeaders.Any(h => h.Value.Span.Contains("compress", StringComparison.OrdinalIgnoreCase)))
                return "compress";

            // Unknown / non-native encoding (e.g. zstd) — return the raw token so a registered
            // IContentDecoder can be resolved through ContentDecoderRegistry. Only Content-Encoding
            // carries a content codec; Transfer-Encoding tokens such as "chunked" are framing, not
            // compression, and must NOT be treated as decodable encodings.
            foreach (var header in encodingHeaders) {
                if (!header.Name.Span.Equals("Content-encoding", StringComparison.OrdinalIgnoreCase))
                    continue;

                var value = header.Value.ToString().Trim();

                if (value.Length > 0
                    && !value.Equals("identity", StringComparison.OrdinalIgnoreCase)
                    && !value.Equals("chunked", StringComparison.OrdinalIgnoreCase))
                    return value.ToLowerInvariant();
            }

            return null;
        }
    }

    public enum CompressionType
    {
        None,
        Gzip,
        Deflate,
        Compress,
        Brotli
    }
}
