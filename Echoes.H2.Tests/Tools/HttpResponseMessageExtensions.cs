// Copyright © 2022 Haga Rakotoharivelo

using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Sandbox.Models;

namespace Echoes.H2.Tests.Tools;

public static class HttpResponseMessageExtensions
{
    public static async Task<HealthCheckResult> GetCheckResult(this HttpResponseMessage message)
    {
        var resultText = await message.Content.ReadAsStringAsync();
        var res = JsonSerializer.Deserialize<HealthCheckResult>(resultText, new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true
        } );

        return res; 
    }

    
}