// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.IO.Compression;
using Fluxzy.Misc.Streams;

namespace Fluxzy.Extensions
{
    public static class CompressionHelper
    {
        public static byte[]? ReadResponseBodyContent(this
            ExchangeInfo exchangeInfo,
            Stream responseBodyInStream, int maximumLength, out CompressionInfo compressionInfo)
        {
            var workStream = GetDecodedResponseBodyStream(exchangeInfo, responseBodyInStream, out var compressionType);

            var encodingToken = exchangeInfo.GetResponseContentEncoding();

            compressionInfo = new CompressionInfo {
                CompressionName = compressionType != CompressionType.None
                    ? compressionType.ToString()
                    : encodingToken ?? CompressionType.None.ToString(),
                EncodingToken = encodingToken
            };

            try {
                return workStream.ReadMaxLengthOrNull(maximumLength);
            }
            finally {
                workStream.Dispose();
            }
        }

        /// <summary>
        ///     Wraps a stored response body with the decoder matching its content-encoding.
        ///     Bodies handed to this method are already de-framed: the proxy strips chunked
        ///     transfer coding before recording, so a Transfer-Encoding header in the exchange
        ///     only describes the wire format and is ignored here.
        /// </summary>
        /// <param name="skipForwarded">Unused, kept for binary compatibility.</param>
        public static Stream GetDecodedResponseBodyStream(this
            IExchange exchangeInfo, Stream responseBodyInStream,
            out CompressionType compressionType, bool skipForwarded = false)
        {
            var workStream = responseBodyInStream;

            var encodingToken = exchangeInfo.GetResponseContentEncoding();
            compressionType = ExchangeExtensions.TokenToCompressionType(encodingToken);

            workStream = GetDecodedStream(encodingToken, workStream);

            return workStream;
        }

        /// <summary>
        ///     Request counterpart of <see cref="GetDecodedResponseBodyStream" />. The stored body
        ///     is never chunked, so only content-encoding is undone.
        /// </summary>
        /// <param name="skipForwarded">Unused, kept for binary compatibility.</param>
        public static Stream GetDecodedRequestBodyStream(this
            IExchange exchangeInfo, Stream requestBodyStream,
            out CompressionType compressionType, bool skipForwarded = false)
        {
            var workStream = requestBodyStream;

            var encodingToken = exchangeInfo.GetRequestContentEncoding();
            compressionType = ExchangeExtensions.TokenToCompressionType(encodingToken);

            workStream = GetDecodedStream(encodingToken, workStream);

            return workStream;
        }

        internal static Stream GetUnChunkedStream(Stream workStream)
        {
            workStream = new ChunkedTransferReadStream(workStream, true);

            return workStream;
        }

        internal static Stream GetDecodedStream(CompressionType compressionType, Stream workStream)
        {
            return GetDecodedStream(CompressionTypeToToken(compressionType), workStream);
        }

        /// <summary>
        ///     Wraps <paramref name="workStream" /> with a decoding stream for the given content-encoding token.
        ///     gzip, deflate and brotli are decoded natively; any other non-empty token is resolved through
        ///     <see cref="ContentDecoderRegistry" />. A <see cref="FluxzyException" /> is thrown when no decoder
        ///     is registered for the encoding.
        /// </summary>
        internal static Stream GetDecodedStream(string? encodingToken, Stream workStream)
        {
            if (string.IsNullOrEmpty(encodingToken))
                return workStream;

            switch (encodingToken) {
                case "gzip":
                    return new GZipStream(workStream, CompressionMode.Decompress, false);

                case "deflate":
                    return new DeflateStream(workStream, CompressionMode.Decompress, false);

                case "br":
                case "brotli":
                    return new BrotliStream(workStream, CompressionMode.Decompress, false);

                default:
                    if (ContentDecoderRegistry.TryGet(encodingToken, out var decoder))
                        return decoder.GetDecodedStream(workStream);

                    throw new FluxzyException(
                        $"No decoder registered for content-encoding '{encodingToken}'. " +
                        $"gzip, deflate and brotli are supported out of the box; register a decoder for " +
                        $"other encodings via ContentDecoderRegistry.Register(...).");
            }
        }

        private static string? CompressionTypeToToken(CompressionType compressionType)
        {
            switch (compressionType) {
                case CompressionType.Gzip:
                    return "gzip";
                case CompressionType.Deflate:
                    return "deflate";
                case CompressionType.Brotli:
                    return "br";
                case CompressionType.Compress:
                    return "compress";
                default:
                    return null;
            }
        }
    }

    public class CompressionInfo
    {
        public string CompressionName { get; set; } = CompressionType.None.ToString();

        /// <summary>
        ///     The raw content-encoding token (lowercased, e.g. "gzip", "br", "zstd"), or null when not encoded.
        ///     Used to resolve a decoder, including those provided through <see cref="ContentDecoderRegistry" />.
        /// </summary>
        public string? EncodingToken { get; set; }
    }
}
