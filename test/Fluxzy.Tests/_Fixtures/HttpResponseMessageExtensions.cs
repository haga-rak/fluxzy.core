// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using fluxzy.sandbox.models;

namespace Fluxzy.Tests._Fixtures
{
    public static class HttpResponseMessageExtensions
    {
        public static async Task<HealthCheckResult> GetCheckResult(this HttpResponseMessage message)
        {
            var resultText = await message.Content.ReadAsStringAsync();

            var res = JsonSerializer.Deserialize<HealthCheckResult>(resultText, new JsonSerializerOptions {
                PropertyNameCaseInsensitive = true
            });

            return res!;
        }
    }
}
