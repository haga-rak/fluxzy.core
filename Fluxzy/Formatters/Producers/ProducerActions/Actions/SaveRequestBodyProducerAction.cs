using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Fluxzy.Formatters.Producers.Requests;

namespace Fluxzy.Formatters.Producers.ProducerActions.Actions
{
    
    public class SaveRequestBodyProducerAction
    {
        private readonly ProducerFactory _producerFactory;

        public SaveRequestBodyProducerAction(ProducerFactory producerFactory)
        {
            _producerFactory = producerFactory;
        }

        public async Task<bool> Do(int exchangeId,  string filePath)
        {
            var context = await _producerFactory.GetProducerContext(
                exchangeId
            );

            if (context is null)
                return false; 


            await using var stream = context.ArchiveReader.GetRequestBody(exchangeId);

            if (stream is null)
                return false;

            var fileInfo = new FileInfo(filePath);

            fileInfo.Directory?.Create();

            await using var outStream = fileInfo.Create();

            await stream.CopyToAsync(outStream);

            return true;
        }
    }
}