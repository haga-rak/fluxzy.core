// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using MessagePack;
using System.Text.Json.Serialization;

namespace Fluxzy
{
    [MessagePackObject]
    public class ExchangeIdentifiersInfo
    {
        public ExchangeIdentifiersInfo(int connectionId, int id)
        {
            ConnectionId = connectionId;
            Id = id;
        }

        [JsonPropertyOrder(-10)]
        [Key(0)]
        public int ConnectionId { get; }

        [JsonPropertyOrder(-9)]
        [Key(1)]
        public int Id { get; }
    }
}
