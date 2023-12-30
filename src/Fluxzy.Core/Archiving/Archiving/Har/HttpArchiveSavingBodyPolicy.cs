// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Text.Json.Serialization;

namespace Fluxzy.Archiving.Har
{
    [JsonConverter(typeof(JsonStringEnumConverter<HttpArchiveSavingBodyPolicy>))]
    public enum HttpArchiveSavingBodyPolicy
    {
        SkipBody = 0,
        MaxLengthSave = 1,
        AlwaysSave = 2
    }
}
