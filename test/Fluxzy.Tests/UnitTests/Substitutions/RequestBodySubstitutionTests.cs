// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients.Headers;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;
using Fluxzy.Misc.Streams;
using Fluxzy.Rules;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Substitutions
{
    public class RequestBodySubstitutionTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public async Task SubstituteBody(int initialPayloadSize)
        {
            var setting = FluxzySetting.CreateDefault();

            using var cancellationTokenSource = new CancellationTokenSource();

            cancellationTokenSource.CancelAfter(TimeoutConstants.Regular * 1000);

            setting.ConfigureRule()
                   .WhenAny()
                   .Do(new RequestBodyMockingAction(Encoding.UTF8.GetBytes("test string")));

            await using var proxy = new Proxy(setting);
            var endPoints = proxy.Run();

            using var httpClient = HttpClientUtility.CreateHttpClient(endPoints, setting);

            var response = await httpClient.PostAsync("https://sandbox.smartizy.com/global-health-check",
                new ByteArrayContent(new byte[initialPayloadSize]), cancellationTokenSource.Token); 

            var body = await response.Content.ReadAsStringAsync(cancellationTokenSource.Token);

            Assert.True(response.IsSuccessStatusCode);
        }
    }

    internal class RequestBodyMockingAction : Action
    {
        private readonly byte[] _data;

        public RequestBodyMockingAction(byte[] data)
        {
            _data = data;
        }

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            var headers = exchange!.Request.Header.HeaderFields.ToList();
            
            context.RequestHeaderAlterations.Add(new HeaderAlterationAdd("Content-type", "text/plain"));
            context.RequestHeaderAlterations.Add(new HeaderAlterationReplace("Content-length",
                _data.Length.ToString(), true));

            context.RegisterRequestBodySubstitution(new RequestBodySubstitution(_data));

            return default;
        }

        public override FilterScope ActionScope => FilterScope.RequestHeaderReceivedFromClient;

        public override string DefaultDescription { get; } = "Test mock";
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
