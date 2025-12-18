// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients.Headers;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;
using Fluxzy.Misc.Streams;
using Fluxzy.Rules;
using Xunit;
using Fluxzy.Tests.Sandbox.Models;

namespace Fluxzy.Tests.UnitTests.Substitutions
{
    public class RequestBodySubstitutionTests
    {
        [Theory]
        [CombinatorialData]
        public async Task SubstituteBody(
            [CombinatorialValues(0, 1)] int initialPayloadSize,
            [CombinatorialValues("", "test string")]
            string testString,
            [CombinatorialValues(false, true)]
            bool withContentLength)
        {
            var setting = FluxzySetting.CreateLocalRandomPort();

            using var cancellationTokenSource = new CancellationTokenSource();

            cancellationTokenSource.CancelAfter(TimeoutConstants.Regular * 1000);

            setting.ConfigureRule()
                   .WhenAny()
                   .Do(new RequestBodyMockingAction(Encoding.UTF8.GetBytes(testString), withContentLength));

            await using var proxy = new Proxy(setting);
            var endPoints = proxy.Run();

            using var httpClient = HttpClientUtility.CreateHttpClient(endPoints, setting);

            var response = await httpClient.PostAsync("https://sandbox.fluxzy.io/global-health-check",
                new ByteArrayContent(new byte[initialPayloadSize]), cancellationTokenSource.Token);

            var body = await response.Content.ReadAsStringAsync(cancellationTokenSource.Token);

            var checkResult = JsonSerializer.Deserialize<HealthCheckResult>(body, new JsonSerializerOptions {
                PropertyNameCaseInsensitive = true
            });

            Assert.True(response.IsSuccessStatusCode);
            Assert.NotNull(checkResult);
            Assert.Equal(testString.Length, checkResult.RequestContent.Length);
        }
    }

    internal class RequestBodyMockingAction : Action
    {
        private readonly byte[] _data;
        private readonly bool _withContentLength;

        public RequestBodyMockingAction(byte[] data, bool withContentLength)
        {
            _data = data;
            _withContentLength = withContentLength;
        }

        public override FilterScope ActionScope => FilterScope.RequestHeaderReceivedFromClient;

        public override string DefaultDescription { get; } = "Test mock";

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            context.RequestHeaderAlterations.Add(new HeaderAlterationReplace("Content-type", "text/plain", true));

            if (_withContentLength) {
                context.RequestHeaderAlterations.Add(new HeaderAlterationReplace("Content-length",
                    _data.Length.ToString(), true));
            }

            context.RegisterRequestBodySubstitution(new RequestBodySubstitution(_data));

            return default;
        }
    }

    internal class RequestBodySubstitution : IStreamSubstitution
    {
        private readonly byte[] _data;

        public RequestBodySubstitution(byte[] data)
        {
            _data = data;
        }

        public async ValueTask<Stream> Substitute(Stream originalStream)
        {
            await originalStream.DrainAsync();

            var memoryStream = new MemoryStream(_data);

            return memoryStream;
        }
    }
}
