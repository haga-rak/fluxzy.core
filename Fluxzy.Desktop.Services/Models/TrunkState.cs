using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using Reinforced.Typings.Attributes;

namespace Fluxzy.Desktop.Services.Models
{
    public class TrunkState
    {
        public TrunkState(
            IEnumerable<ExchangeContainer> internalExchanges,
            IEnumerable<ConnectionContainer> internalConnections)
        {
            Exchanges = internalExchanges.OrderBy(r => r.Id).ToImmutableList();
            Connections = internalConnections.OrderBy(r => r.Id).ToImmutableList();
            
            for (int index = 0; index < Exchanges.Count; index++)
            {
                var exchange = Exchanges[index];
                ExchangesIndexer[exchange.Id] = index;
                MaxExchangeId = exchange.Id; 
            }

            for (var index = 0; index < Connections.Count; index++)
            {
                var connection = Connections[index];
                ConnectionsIndexer[connection.Id] = index;
                MaxConnectionId = connection.Id;
            }
        }

        public ImmutableList<ExchangeContainer> Exchanges { get; }


        public ImmutableList<ConnectionContainer> Connections { get; }


        public int MaxExchangeId { get;  }

        public int MaxConnectionId { get;  }

        /// <summary>
        /// Map a exchange Id to its position (index) on Exchanges list
        /// </summary>
        public Dictionary<int, int> ExchangesIndexer { get; } = new();

        /// <summary>
        /// same algorithm as ExchangesIndexer 
        /// </summary>
        public Dictionary<int, int> ConnectionsIndexer { get; } = new();

        public static TrunkState Empty()
        {
            return new TrunkState(Array.Empty<ExchangeContainer>(), Array.Empty<ConnectionContainer>()); 
        }
    }
}