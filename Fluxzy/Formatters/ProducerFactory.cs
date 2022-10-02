// Copyright © 2022 Haga Rakotoharivelo

using System.Collections.Generic;
using Fluxzy.Formatters.Producers;
using Fluxzy.Formatters.Producers.Requests;
using Fluxzy.Readers;
using Fluxzy.Screeners;

namespace Fluxzy.Formatters
{
    public class ProducerFactory
    {
        private readonly List<IFormattingProducer<FormattingResult>> _requestProducers = new()
        {
            new RequestJsonBodyProducer(),
            new AuthorizationBasicProducer(),
            new AuthorizationBearerProducer(),
            new QueryStringProducer(),
            new RequestCookieProducer(),
            new RequestBodyAnalysis(),
            new RequestTextBodyProducer(),
            new AuthorizationProducer(),
            new RawRequestHeaderProducer(),
        };

        public IEnumerable<FormattingResult> GetRequestFormattedResults(int exchangeId, IArchiveReader archiveReader,
            ProducerSettings settings)
        {
            var exchangeInfo = archiveReader.ReadExchange(exchangeId);

            if (exchangeInfo == null)
            {
                yield break;
            }

            using var formattingProducerContext = new FormattingProducerContext(exchangeInfo, archiveReader, settings);

            foreach (var producer in _requestProducers)
            {
                var result = producer.Build(exchangeInfo, formattingProducerContext);

                if (result != null)
                    yield return result;
            }
        }
    }
}