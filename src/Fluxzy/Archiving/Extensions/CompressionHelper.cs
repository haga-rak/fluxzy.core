// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.IO;
using System.IO.Compression;
using Fluxzy.Misc.Streams;
using ICSharpCode.SharpZipLib.Lzw;

namespace Fluxzy.Extensions
{
    public static class CompressionHelper
    {
        public static byte[]? ReadResponseBodyContent(
            ExchangeInfo exchangeInfo,
            Stream responseBodyInStream, int maximumLength, out CompressionInfo compressionInfo)
        {
            // Check for chunked body 
            var workStream = GetDecodedContentStream(exchangeInfo, responseBodyInStream, out var compressionType);

            compressionInfo = new CompressionInfo {
                CompressionName = compressionType.ToString()
            };

            try {
                return workStream.ReadMaxLengthOrNull(maximumLength);
            }
            finally {
                workStream.Dispose();
            }
        }

        public static Stream GetDecodedContentStream(
            ExchangeInfo exchangeInfo, Stream responseBodyInStream,
            out CompressionType compressionType, bool skipForwarded = false)
        {
            var workStream = responseBodyInStream;

            if (exchangeInfo.IsChunkedTransferEncoded(skipForwarded))
                workStream = new ChunkedTransferReadStream(workStream, true);

            compressionType = exchangeInfo.GetCompressionType();

            switch (compressionType) {
                case CompressionType.None:
                    break;

                case CompressionType.Gzip:
                    workStream = new GZipStream(workStream, CompressionMode.Decompress, false);

                    break;

                case CompressionType.Deflate:
                    workStream = new DeflateStream(workStream, CompressionMode.Decompress, false);

                    break;

                case CompressionType.Compress:
                    workStream = new LzwInputStream(workStream);

                    break;

                case CompressionType.Brotli:
                    workStream = new BrotliStream(workStream, CompressionMode.Decompress, false);

                    break;
            }

            return workStream;
        }
        
    }

    public class CompressionInfo
    {
        public string CompressionName { get; set; } = CompressionType.None.ToString();
    }
}
