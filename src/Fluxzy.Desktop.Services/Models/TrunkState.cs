// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Desktop.Services.Models
{
    public class TrunkState
    {
        public TrunkState(
            List<ExchangeContainer> internalExchanges,
            List<ConnectionContainer> internalConnections, int errorCount)
        {
            Exchanges = internalExchanges;
            Connections = internalConnections;
            ErrorCount = errorCount;
            var agents = new HashSet<Agent>();

            for (var index = 0; index < Exchanges.Count; index++) {
                var exchange = Exchanges[index];
                ExchangesIndexer[exchange.Id] = index;
                MaxExchangeId = exchange.Id;

                var exchangeInfo = (ExchangeInfo) exchange.ExchangeInfo;

                if (exchangeInfo.Agent != null)
                    agents.Add(exchangeInfo.Agent);

                // Here we trigger the contextual filter
            }

            for (var index = 0; index < Connections.Count; index++) {
                var connection = Connections[index];
                ConnectionsIndexer[connection.Id] = index;
                MaxConnectionId = connection.Id;
            }

            Agents = agents.OrderBy(r => r.FriendlyName).ToList();
        }

        public TrunkState(
            IEnumerable<ExchangeContainer> internalExchanges,
            IEnumerable<ConnectionContainer> internalConnections, int errorCount)
            : this(internalExchanges.OrderBy(r => r.Id).ToList(), internalConnections.OrderBy(r => r.Id).ToList(), errorCount)
        {
        }

        public List<ExchangeContainer> Exchanges { get; }

        public List<ConnectionContainer> Connections { get; }

        public int ErrorCount { get; }

        public List<Agent> Agents { get; set; }

        public int MaxExchangeId { get; }

        public int MaxConnectionId { get; }

        /// <summary>
        ///     Map a exchange Identifier to its position (index) on Exchanges list
        /// </summary>
        public Dictionary<int, int> ExchangesIndexer { get; } = new();

        /// <summary>
        ///     same algorithm as ExchangesIndexer
        /// </summary>
        public Dictionary<int, int> ConnectionsIndexer { get; } = new();

        public static TrunkState Empty()
        {
            return new TrunkState(Array.Empty<ExchangeContainer>(), Array.Empty<ConnectionContainer>(), 0);
        }

        public TrunkState ApplyFilter(FilteredExchangeState state, int errorCount)
        {
            return new TrunkState(Exchanges.Where(e => state.Exchanges.Contains(e.Id)), Connections, errorCount);
        }
    }
}
