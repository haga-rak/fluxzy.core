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
        public Dictionary<string, string>? Headers { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("data")]
        public string? Data { get; set; }

        public Memory<byte> GetBinary(int length)
        {
            if (string.IsNullOrWhiteSpace(Data))
                return default;

            var prefix = "data:application/octet-stream;base64,";

            if (!Data.StartsWith(prefix))
                return default;

            var dataSpan = Data.AsSpan(prefix.Length);

            Memory<byte> memory = new byte[length];

            if (!Convert.TryFromBase64Chars(dataSpan, memory.Span, out _))
            {
                throw new InvalidOperationException("Invalid length"); 
            }

            return memory;
        }
    }

    public static class HttpConstants
    {
        public static readonly HashSet<string> PermanentHeaders = new(StringComparer.OrdinalIgnoreCase)
        {
            "host"
        }; 
    }
}
