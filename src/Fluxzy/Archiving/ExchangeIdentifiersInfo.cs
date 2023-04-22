// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Text.Json.Serialization;

namespace Fluxzy
{
    public class ExchangeIdentifiersInfo
    {
        public ExchangeIdentifiersInfo(int connectionId, int id)
        {
            ConnectionId = connectionId;
            Id = id;
        }

        [JsonPropertyOrder(-10)]
        public int ConnectionId { get; }

        [JsonPropertyOrder(-9)]
        public int Id { get; }
    }
}
