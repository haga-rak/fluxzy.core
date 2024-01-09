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

        public static Encoding? GetResponseEncoding(this ExchangeInfo exchangeInfo)
        {
            if (exchangeInfo.ResponseHeader?.Headers == null)
                return null;

            var contentTypeHeader = exchangeInfo.ResponseHeader.Headers.LastOrDefault(h =>
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
            var headers = exchangeInfo.GetResponseHeaders();

            if (headers == null)
                return CompressionType.None; 
            
            return InternalGetCompressionType(headers);
        }

        public static CompressionType GetRequestCompressionType(this IExchange exchangeInfo)
        {
            var headers = exchangeInfo.GetRequestHeaders();

            if (headers == null)
                throw new InvalidOperationException("This exchange does not have request body");

            return InternalGetCompressionType(headers);
        }

        private static CompressionType InternalGetCompressionType(IEnumerable<HeaderFieldInfo> headers)
        {
            var encodingHeaders = headers.Where(h => (h.Forwarded &&
                                                      h.Name.Span.Equals("Transfer-Encoding",
                                                          StringComparison.OrdinalIgnoreCase))
                                                     || h.Name.Span.Equals("Content-encoding",
                                                         StringComparison.OrdinalIgnoreCase)).ToList();

            if (encodingHeaders.Any(h => h.Value.Span.Contains("gzip", StringComparison.OrdinalIgnoreCase)))
                return CompressionType.Gzip;

            if (encodingHeaders.Any(h => h.Value.Span.Contains("deflate", StringComparison.OrdinalIgnoreCase)))
                return CompressionType.Deflate;

            if (encodingHeaders.Any(h => h.Value.Span.Contains("br", StringComparison.OrdinalIgnoreCase)))
                return CompressionType.Brotli;

            if (encodingHeaders.Any(h => h.Value.Span.Contains("compress", StringComparison.OrdinalIgnoreCase)))
                return CompressionType.Compress;

            return CompressionType.None;
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
