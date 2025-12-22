// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Linq;
using System.Threading.Tasks;
using Fluxzy.Formatters;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Formatters
{
    public class ProducerContextTest : ProduceDeletableItem
    {
        [Fact]
        public async Task ProducerFactoryCreate()
        {
            var randomFile = GetRegisteredRandomFile();
            var url = "https://sandbox.fluxzy.io/ip?a=1&b=2&c=3";

            await QuickArchiveBuilder.MakeQuickArchiveGet(url, randomFile);

            var archiveReaderProvider = new FromFileArchiveFileProvider(randomFile);
            var archiveReader = (await archiveReaderProvider.Get())!;

            var producerFactory = new ProducerFactory(archiveReaderProvider, FormatSettings.Default);
            var firstExchange = archiveReader.ReadAllExchanges().First();

            using var context = await producerFactory.GetProducerContext(firstExchange.Id);
            
            Assert.NotNull(context);
            Assert.NotNull(context.GetContextInfo());
            Assert.NotNull(context.GetContextInfo().ResponseBodyLength);
            Assert.NotNull(context.GetContextInfo().ResponseBodyText);
            Assert.True(context.GetContextInfo().IsTextContent);
            
            _ = producerFactory.GetRequestFormattedResults(firstExchange.Id, context).ToList();
            var responses = producerFactory.GetResponseFormattedResults(firstExchange.Id, context).ToList();
            
            Assert.NotEmpty(responses);

            foreach (var response in responses) {
                Assert.NotNull(response.Title);
                Assert.NotNull(response.Type);
                Assert.Null(response.ErrorMessage);
            }
        }
    }
}
