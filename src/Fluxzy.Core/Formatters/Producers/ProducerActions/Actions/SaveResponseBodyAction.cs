// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Threading.Tasks;
using Fluxzy.Extensions;

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

            var encodingToken = context.CompressionInfo?.EncodingToken;

            if (string.IsNullOrEmpty(encodingToken)) {
                await rawFileStream.CopyToAsync(outStream);

                return true;
            }

            // gzip/deflate/brotli are decoded natively; other encodings (e.g. compress/LZW, zstd) require a
            // decoder registered via ContentDecoderRegistry, otherwise a FluxzyException is thrown.
            var resultStream = CompressionHelper.GetDecodedStream(encodingToken, rawFileStream);

            await resultStream.CopyToAsync(outStream);

            return true;
        }
    }
}
