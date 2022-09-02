﻿using System.Text.Json.Serialization;

namespace Fluxzy.Desktop.Services.Models
{
    public class TrunkState
    {
        public TrunkState(TrunkState copy)
        {
            Exchanges = copy.Exchanges.ToList();
            Connections = copy.Connections.ToList();
            ExchangeIndex = ExchangeIndex.ToDictionary(t => t.Key, t => t.Value); 
        }

        public TrunkState(List<ExchangeContainer> exchanges, List<ConnectionContainer> connections)
        {
            Exchanges = exchanges;
            Connections = connections;
        }

        public List<ExchangeContainer> Exchanges { get; }

        public List<ConnectionContainer> Connections { get; }

        /// <summary>
        /// Used at client level 
        /// </summary>
        [JsonIgnore]
        public Dictionary<int, ExchangeContainer> ExchangeIndex { get; } = new();

        public static TrunkState Empty()
        {
            return new TrunkState(new(), new()); 
        }
    }
}