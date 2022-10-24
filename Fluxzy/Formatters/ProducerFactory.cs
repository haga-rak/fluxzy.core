// Copyright © 2022 Haga Rakotoharivelo

using System.Collections.Generic;
using System.Threading.Tasks;
using Fluxzy.Formatters.Producers.Requests;
using Fluxzy.Formatters.Producers.Responses;

namespace Fluxzy.Formatters
{
    public class ProducerFactory
    {
        private readonly IArchiveReaderProvider _archiveReaderProvider;
        private readonly ProducerSettings _producerSettings;

        private static readonly List<IFormattingProducer<FormattingResult>> RequestProducers = new()
        {
            new RequestJsonBodyProducer(),
            new MultipartFormContentProducer(),
            new FormUrlEncodedProducer(),
            new QueryStringProducer(),
            new RequestBodyAnalysis(),
            new AuthorizationBasicProducer(),
            new AuthorizationBearerProducer(),
            new RequestCookieProducer(),
            new RequestTextBodyProducer(),
            new AuthorizationProducer(),
            new RawRequestHeaderProducer(),
        };

        private static readonly List<IFormattingProducer<FormattingResult>> ResponseProducers = new()
        {
            new ResponseBodySummaryProducer(),
            new ResponseBodyJsonProducer(),
            new SetCookieProducer(),
            new ResponseTextContentProducer(),
            new WsMessageProducer()
        };

        public ProducerFactory(IArchiveReaderProvider archiveReaderProvider, ProducerSettings producerSettings)
        {
            _archiveReaderProvider = archiveReaderProvider;
            _producerSettings = producerSettings;
        }

        public async Task<ProducerContext?> GetProducerContext(int exchangeId)
        {
            var archiveReader = await _archiveReaderProvider.Get();

            if (archiveReader == null)
                return null;  

            var exchangeInfo = archiveReader.ReadExchange(exchangeId);

            if (exchangeInfo == null)
            {
                return null;
            }

            return new ProducerContext(exchangeInfo, archiveReader, _producerSettings);
        }

        public IEnumerable<FormattingResult> GetRequestFormattedResults(int exchangeId, ProducerContext formattingProducerContext)
        {
            foreach (var producer in RequestProducers)
            {
                var result = producer.Build(formattingProducerContext.Exchange, formattingProducerContext);

                if (result != null)
                    yield return result;
            }
        }

        public  IEnumerable<FormattingResult> GetResponseFormattedResults(int exchangeId, ProducerContext formattingProducerContext)
        {
            foreach (var producer in ResponseProducers)
            {
                var result = producer.Build(formattingProducerContext.Exchange, formattingProducerContext);

                if (result != null)
                    yield return result;
            }
        }
    }

    
}