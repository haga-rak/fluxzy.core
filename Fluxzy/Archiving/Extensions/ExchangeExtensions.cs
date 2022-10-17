using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Fluxzy.Extensions
{
    public static class ExchangeExtensions
    {



        public static string? GetResponseHeaderValue(this ExchangeInfo exchangeInfo, string headerName)
        {
            if (exchangeInfo.ResponseHeader?.Headers == null)
                return null;

            var contentTypeHeader = exchangeInfo.ResponseHeader.Headers.LastOrDefault(h =>
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

            if (matchResult.Success)
            {
                try
                {
                    return Encoding.GetEncoding(matchResult.Groups[1].Value);
                }
                catch (ArgumentException)
                {

                }
            }

            return null; 
        }

        public static bool IsChunkedTransferEncoded(this ExchangeInfo exchangeInfo)
        {
            if (exchangeInfo.ResponseHeader?.Headers == null)
                return false;

            return exchangeInfo.ResponseHeader.Headers.Any(h => !h.Forwarded &&
                h.Name.Span.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase)
                && h.Value.Span.Equals("chunked", StringComparison.OrdinalIgnoreCase)
            ); 
        }

        public static CompressionType GetCompressionType(this ExchangeInfo exchangeInfo)
        {
            if (exchangeInfo.ResponseHeader?.Headers == null)
                throw new InvalidOperationException($"This exchange does not have response body");

            var encodingHeaders = exchangeInfo.ResponseHeader.Headers.Where(h => h.Forwarded &&
                h.Name.Span.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase)
                || h.Name.Span.Equals("Content-encoding", StringComparison.OrdinalIgnoreCase)).ToList();

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
        Brotli,
    }
}
