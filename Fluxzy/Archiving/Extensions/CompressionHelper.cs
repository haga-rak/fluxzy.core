// Copyright © 2022 Haga Rakotoharivelo

using System.IO;
using System.IO.Compression;
using Fluxzy.Misc.Streams;
using ICSharpCode.SharpZipLib.Lzw;

namespace Fluxzy.Extensions
{
    public static class CompressionHelper
    {
        public static byte []?  ReadContent(
            ExchangeInfo exchangeInfo, 
            Stream responseBodyInStream, int maximumLength, out CompressionInfo compressionInfo)
        {
            // Check for chunked body 
            var workStream = responseBodyInStream;

            if (exchangeInfo.IsChunkedTransferEncoded())
            {
                workStream = new ChunkedTransferReadStream(workStream, false); 
            }

            var compressionType = exchangeInfo.GetCompressionType();

            switch (compressionType)
            {
                case CompressionType.None:
                    break;
                case CompressionType.Gzip:
                    workStream = new GZipStream(workStream, CompressionMode.Decompress, true);
                    break;
                case CompressionType.Deflate:
                    workStream = new DeflateStream(workStream, CompressionMode.Decompress, true);
                    break;
                case CompressionType.Compress:
                    workStream = new LzwInputStream(workStream);
                    break;
                case CompressionType.Brotli:
                    workStream = new BrotliStream(workStream, CompressionMode.Decompress, true);
                    break;
            }

            compressionInfo = new CompressionInfo()
            {
                CompressionName = compressionType.ToString()
            };

            try
            {

                return workStream.ReadMaxLengthOrNull(maximumLength);
            }
            finally
            {
                workStream.Dispose();
            }
        }
    }

    public class CompressionInfo
    {
        public string CompressionName { get; set; } = CompressionType.None.ToString();
    }
}