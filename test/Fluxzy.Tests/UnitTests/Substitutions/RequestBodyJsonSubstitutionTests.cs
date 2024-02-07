// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Fluxzy.Clients.Headers;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;
using Fluxzy.Rules;
using Xunit;
using Action = Fluxzy.Rules.Action;

namespace Fluxzy.Tests.UnitTests.Substitutions
{
    public class ModifyJsonSubstitutionTests
    {
        [Fact]
        public async Task SubstituteJsonBody()
        {
            var setting = FluxzySetting.CreateLocalRandomPort();

            setting.ConfigureRule()
                   .WhenAny()
                   .Do(new RequestBodyJsonMockingAction());

            await using var proxy = new Proxy(setting);
            var endPoints = proxy.Run();

            using var httpClient = HttpClientUtility.CreateHttpClient(endPoints, setting);

            var payload = new DummyPayload {
                DoYouAgree = "YES"
            };

            var response = await httpClient.PostAsJsonAsync("https://httpbin.org/anything", payload);

            var httpBinResult = await response.Content.ReadFromJsonAsync<HttpBinResponse>();
            var modifiedPayload = JsonSerializer.Deserialize<DummyPayload>(httpBinResult!.Data!);

            Assert.True(response.IsSuccessStatusCode);
            Assert.NotNull(modifiedPayload!);
            Assert.Equal(modifiedPayload.DoYouAgree, "NO", StringComparer.OrdinalIgnoreCase);
        }
    }

    internal class HttpBinResponse
    {
        [JsonPropertyName("data")]
        public string? Data { get; set; }
    }

    internal class RequestBodyJsonMockingAction : Action
    {
        public override FilterScope ActionScope => FilterScope.RequestHeaderReceivedFromClient;

        public override string DefaultDescription => "Test mock with JSON";

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            context.RequestHeaderAlterations.Add(new HeaderAlterationDelete("Content-length"));
            context.RegisterRequestBodySubstitution(new ModifyJsonSubstitution());

            return default;
        }
    }

    internal class ModifyJsonSubstitution : IStreamSubstitution
    {
        public async ValueTask<Stream> Substitute(Stream originalStream)
        {
            var result = (await JsonSerializer.DeserializeAsync<DummyPayload>(originalStream))!;
            result.DoYouAgree = "NO";

            return new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(result));
        }
    }

    internal class DummyPayload
    {
        public string DoYouAgree { get; set; } = "";
    }
}
