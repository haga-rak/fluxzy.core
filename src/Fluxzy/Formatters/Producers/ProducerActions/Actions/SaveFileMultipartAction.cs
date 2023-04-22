// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.IO;
using System.Threading.Tasks;
using Fluxzy.Formatters.Producers.Requests;

namespace Fluxzy.Formatters.Producers.ProducerActions.Actions
{
    public class SaveFileMultipartAction
    {
        private readonly ProducerFactory _producerFactory;

        public SaveFileMultipartAction(ProducerFactory producerFactory)
        {
            _producerFactory = producerFactory;
        }

        public async Task<bool> Do(int exchangeId, SaveFileMultipartActionModel model)
        {
            var context = await _producerFactory.GetProducerContext(
                exchangeId
            );

            if (context is null)
                return false;

            await using var stream = context.ArchiveReader.GetRequestBody(exchangeId);

            if (stream is null)
                return false;

            var fileInfo = new FileInfo(model.FilePath);

            fileInfo.Directory?.Create();

            await using var outStream = fileInfo.Create();

            await using var contentStream = stream.GetSlicedStream(model.Offset, model.Length);

            await contentStream.CopyToAsync(outStream);

            return true;
        }
    }

    public class SaveFileMultipartActionModel
    {
        public SaveFileMultipartActionModel(string filePath, long offset, long length)
        {
            FilePath = filePath;
            Offset = offset;
            Length = length;
        }

        public string FilePath { get; }

        public long Offset { get; }

        public long Length { get; }
    }
}
