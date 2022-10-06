using System;
using System.Linq;

namespace Fluxzy.Extensions
{
    public static class ExchangeExtensions
    {
        public static bool IsChunkedTransferEncoded(this ExchangeInfo exchangeInfo)
        {
            if (exchangeInfo.ResponseHeader?.Headers == null)
                return false;

            return exchangeInfo.ResponseHeader.Headers.Any(h =>
                h.Name.Span.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase)
                && h.Value.Span.Equals("chunked", StringComparison.OrdinalIgnoreCase)
            ); 
        }

        public static CompressionType GetCompressionType(this ExchangeInfo exchangeInfo)
        {
            if (exchangeInfo.ResponseHeader?.Headers == null)
                throw new InvalidOperationException($"This exchange does not have response body");

            var encodingHeaders = exchangeInfo.ResponseHeader.Headers.Where(h =>
                h.Name.Span.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase)
                || h.Name.Span.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase)).ToList();

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
