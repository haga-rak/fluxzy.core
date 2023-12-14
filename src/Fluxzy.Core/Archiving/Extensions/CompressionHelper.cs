// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.IO;
using System.IO.Compression;
using Fluxzy.Misc.Streams;
using ICSharpCode.SharpZipLib.Lzw;

namespace Fluxzy.Extensions
{
    public static class CompressionHelper
    {
        public static byte[]? ReadResponseBodyContent(this
            ExchangeInfo exchangeInfo,
            Stream responseBodyInStream, int maximumLength, out CompressionInfo compressionInfo)
        {
            // Check for chunked body 
            var workStream = GetDecodedResponseBodyStream(exchangeInfo, responseBodyInStream, out var compressionType);

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

        public static Stream GetDecodedResponseBodyStream(this 
            IExchange exchangeInfo, Stream responseBodyInStream,
            out CompressionType compressionType, bool skipForwarded = false)
        {
            var workStream = responseBodyInStream;

            if (exchangeInfo.IsResponseChunkedTransferEncoded(skipForwarded))
                workStream = GetUnChunkedStream(workStream);

            compressionType = exchangeInfo.GetResponseCompressionType();

            workStream = GetDecodedStream(compressionType, workStream);

            return workStream;
        }

        public static Stream GetDecodedRequestBodyStream(this 
            IExchange exchangeInfo, Stream requestBodyStream,
            out CompressionType compressionType, bool skipForwarded = false)
        {
            var workStream = requestBodyStream;

            if (exchangeInfo.IsRequestChunkedTransferEncoded(skipForwarded))
                workStream = new ChunkedTransferReadStream(workStream, true);

            compressionType = exchangeInfo.GetRequestCompressionType();

            workStream = GetDecodedStream(compressionType, workStream);

            return workStream;
        }

        internal static Stream GetUnChunkedStream(Stream workStream)
        {
            workStream = new ChunkedTransferReadStream(workStream, true);

            return workStream;
        }


        internal static Stream GetDecodedStream(CompressionType compressionType, Stream workStream)
        {
            switch (compressionType)
            {
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
