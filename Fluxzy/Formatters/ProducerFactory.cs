// Copyright © 2022 Haga Rakotoharivelo

using System.Collections.Generic;
using Fluxzy.Formatters.Producers;
using Fluxzy.Readers;
using Fluxzy.Screeners;

namespace Fluxzy.Formatters
{
    public class ProducerFactory
    {
        private List<IFormattingProducer<FormattingResult>> _requestProducers = new()
        {
            new AuthorizationBasicProducer(),
            new AuthorizationBearerProducer(),
            new AuthorizationProducer(),
            new QueryStringProducer(),
            new RequestCookieProducer(),
            new RequestJsonBodyProducer(),
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