// Copyright © 2021 Haga Rakotoharivelo

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Echoes.H2.Tests;

public class MockResponse
{
    [JsonPropertyName("headers")]
    public Dictionary<string, string> Headers { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }
}