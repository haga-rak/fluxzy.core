// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Fluxzy.Formatters;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Formatters
{
    public class MultipartUploadTest
    {
        [Fact]
        public async Task Validate()
        {
            var fakeBuffer = "helloWorld".ToBytes(Encoding.UTF8);

            using var content = new MultipartFormDataContent
            {
                { new ByteArrayContent(fakeBuffer), "file", "image.png" }
            };

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://sandbox.fluxzy.io/global-health-check")
            {
                Content = content
            };

            var outFileName = "archive.fxzy";

            await QuickArchiveBuilder.MakeQuickArchive(requestMessage, outFileName);

            var archiveReaderProvider = new FromFileArchiveFileProvider(outFileName);
            var archiveReader = (await archiveReaderProvider.Get())!;

            var producerFactory = new ProducerFactory(archiveReaderProvider, FormatSettings.Default);
            var firstExchange = archiveReader.ReadAllExchanges().First();

            using var context = await producerFactory.GetProducerContext(firstExchange.Id);

            Assert.NotNull(context);
            Assert.NotNull(context.GetContextInfo());
            Assert.NotNull(context.GetContextInfo().ResponseBodyLength);
            Assert.NotNull(context.GetContextInfo().ResponseBodyText);
            Assert.True(context.GetContextInfo().IsTextContent);

            var result =  producerFactory.GetRequestFormattedResults(firstExchange.Id, context).ToList();
        }
    }
}
