// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Readers;

namespace Fluxzy.Formatters.Metrics
{
    public class ExchangeMetricBuilder
    {
        public ExchangeMetricInfo? Get(int exchangeId, IArchiveReader reader)
        {
            var exchange = reader.ReadExchange(exchangeId);

            if (exchange == null)
                return null;

            var connectionInfo = reader.ReadConnection(exchange.ConnectionId);

            return new ExchangeMetricInfo(exchangeId, exchange.Metrics, connectionInfo);
        }
    }
}
