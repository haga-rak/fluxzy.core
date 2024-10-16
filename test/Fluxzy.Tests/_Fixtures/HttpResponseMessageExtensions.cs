// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Tests.Sandbox.Models;

namespace Fluxzy.Tests._Fixtures
{
    public static class HttpResponseMessageExtensions
    {
        public static async Task<HealthCheckResult> GetCheckResult(this HttpResponseMessage message, CancellationToken token = default)
        {
            var resultText = await message.Content.ReadAsStringAsync(token);

            var res = JsonSerializer.Deserialize<HealthCheckResult>(resultText, new JsonSerializerOptions {
                PropertyNameCaseInsensitive = true
            });

            return res!;
        }
    }
}
