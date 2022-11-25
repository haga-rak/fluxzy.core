// // Copyright 2022 - Haga Rakotoharivelo
// 

using System.Text.Json.Serialization;

namespace Fluxzy
{
    public class ExchangeIdentifiersInfo
    {
        [JsonPropertyOrder(-10)]
        public int ConnectionId { get; }

        [JsonPropertyOrder(-9)]
        public int Id { get; }
    }
}