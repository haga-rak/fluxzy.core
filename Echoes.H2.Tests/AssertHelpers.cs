// Copyright © 2021 Haga Rakotoharivelo

using System.Net.Http;
using System.Text.Json;
using Echoes.H2.Tests.Bins;
using Xunit;

namespace Echoes.H2.Tests;

public static class AssertHelpers
{
    public static void ControlHeaders(string contentText, HttpRequestMessage requestMessage)
    {
        var binResponse = JsonSerializer.Deserialize<MockResponse>(contentText)!;

        foreach (var header in requestMessage.Headers)
        {
            if (HttpConstants.PermanentHeaders.Contains(header.Key))
                continue;

            Assert.True(binResponse.Headers.TryGetValue(header.Key, out var responseValue));
            Assert.Equal(string.Join(",", header.Value), responseValue);
        }
    }
}