// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Buffers;
using System.IO;
using System.IO.Compression;
using System.Text;
using Fluxzy.Extensions;
using Fluxzy.Misc;
using Fluxzy.Misc.Streams;
using Fluxzy.Readers;
using ICSharpCode.SharpZipLib.Lzw;

namespace Fluxzy.Formatters
{
    public class ProducerContext : IDisposable
    {
        private byte[]?  _internalBuffer; 

        public ProducerContext(
            ExchangeInfo exchange, 
            IArchiveReader archiveReader,
            ProducerSettings settings)
        {
            Exchange = exchange;
            ArchiveReader = archiveReader;
            Settings = settings;
            
            using var requestBodyStream = archiveReader.GetRequestBody(exchange.Id);

            RequestBodyLength = requestBodyStream?.Length ?? 0;

            if (requestBodyStream != null && requestBodyStream.CanSeek && requestBodyStream.Length <
                Settings.MaxFormattableJsonLength)
            {
                _internalBuffer = ArrayPool<byte>.Shared.Rent((int) requestBodyStream.Length);
                int length = requestBodyStream.SeekableStreamToBytes(_internalBuffer);

                RequestBody = new ReadOnlyMemory<byte>(_internalBuffer, 0, length);

                if (ArrayTextUtilities.IsText(RequestBody.Span))
                {
                    RequestBodyText = Encoding.UTF8.GetString(RequestBody.Span);
                }
            }

            using var responseBodyStream = archiveReader.GetResponseBody(exchange.Id);

            if (responseBodyStream != null)
            {
                ResponseContentLength = responseBodyStream.Length;

                ResponseBodyContent = CompressionHelper.ReadContent(exchange, responseBodyStream, settings.MaximumRenderableBodyLength,
                    out var compressionInfo);

                if (ArrayTextUtilities.IsText(ResponseBodyContent, 1024 * 1024))
                {



                    ResponseBodyText = Encoding.UTF8.GetString(RequestBody.Span);
                }

                CompressionInfo = compressionInfo;
            }
        }

        public ExchangeInfo Exchange { get; }

        public IArchiveReader ArchiveReader { get; }

        public ProducerSettings Settings { get; }

        public long RequestBodyLength { get; } = 0;

        public ReadOnlyMemory<byte> RequestBody { get;  }

        public byte []? ResponseBodyContent { get; }

        /// <summary>
        /// If first 1024 utf8 chars are printable char, this property will contains
        /// the decoded UTF8 text
        /// </summary>
        public string ? RequestBodyText { get;  }

        public long? ResponseContentLength { get; }

        public CompressionInfo? CompressionInfo { get; }

        public string? ResponseBodyText { get; }


        public long ResponseBodyLength { get; } = 0;

        public void Dispose()
        {
            if (_internalBuffer != null)
            {
                ArrayPool<byte>.Shared.Return(_internalBuffer);
                _internalBuffer = null; 
            }
        }
    }

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
        public string ? CompressionName { get; set; }
    }
}