// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Fluxzy.Extensions;
using ICSharpCode.SharpZipLib.Lzw;

namespace Fluxzy.Formatters.Producers.ProducerActions.Actions
{
    public class SaveResponseBodyAction
    {
        private readonly ProducerFactory _producerFactory;

        public SaveResponseBodyAction(ProducerFactory producerFactory)
        {
            _producerFactory = producerFactory;
        }

        public async Task<bool> Do(int exchangeId, bool decode, string filePath)
        {
            var context = await _producerFactory.GetProducerContext(exchangeId);

            if (context == null)
                return false;

            await using var rawFileStream = context.ArchiveReader.GetResponseBody(exchangeId);

            if (rawFileStream == null)
                return false;

            await using var outStream = File.Create(filePath);

            if (!decode) {
                await rawFileStream.CopyToAsync(outStream);

                return true;
            }

            if (context.CompressionInfo?.CompressionName == null ||
                context.CompressionInfo?.CompressionName == nameof(CompressionType.None)) {
                await rawFileStream.CopyToAsync(outStream);

                return true;
            }

            var compressionType = Enum.Parse<CompressionType>(context.CompressionInfo!.CompressionName);

            Stream resultStream;

            switch (compressionType) {
                case CompressionType.None:
                    resultStream = rawFileStream;

                    break;

                case CompressionType.Gzip:
                    resultStream = new GZipStream(rawFileStream, CompressionMode.Decompress, true);

                    break;

                case CompressionType.Deflate:
                    resultStream = new DeflateStream(rawFileStream, CompressionMode.Decompress, true);

                    break;

                case CompressionType.Compress:
                    resultStream = new LzwInputStream(rawFileStream);

                    break;

                case CompressionType.Brotli:
                    resultStream = new BrotliStream(rawFileStream, CompressionMode.Decompress, true);

                    break;

                default:
                    resultStream = rawFileStream;

                    break;
            }

            await resultStream.CopyToAsync(outStream);

            return true;
        }
    }
}
