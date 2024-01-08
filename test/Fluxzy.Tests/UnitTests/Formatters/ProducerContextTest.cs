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
            var url = "https://example.com/?a=1&b=2&c=3";

            await QuickArchiveBuilder.MakeQuickArchiveGet(url, randomFile);

            var archiveReaderProvider = new FromFileArchiveFileProvider(randomFile);
            var archiveReader = (await archiveReaderProvider.Get())!;

            var producerFactory = new ProducerFactory(archiveReaderProvider, FormatSettings.Default);
            var firstExchange = archiveReader.ReadAllExchanges().First();

            _ = await producerFactory.GetProducerContext(firstExchange.Id);
        }
    }
}
