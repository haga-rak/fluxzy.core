// Copyright © 2021 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Echoes.H2.Tests.Bins
{
    public class MockResponse
    {
        [JsonConverter(typeof(CaseInsensitiveDictionaryConverter<string>))]
        [JsonPropertyName("headers")]
        public Dictionary<string, string> Headers { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }
    }

    public static class HttpConstants
    {
        public static readonly HashSet<string> PermanentHeaders = new(StringComparer.OrdinalIgnoreCase)
        {
            "host"
        }; 
    }
}
