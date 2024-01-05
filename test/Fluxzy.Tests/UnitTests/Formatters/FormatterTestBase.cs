// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Linq;
using System.Threading.Tasks;
using Fluxzy.Formatters;
using Fluxzy.Tests._Fixtures;

namespace Fluxzy.Tests.UnitTests.Formatters
{
    public class FormatterTestBase : ProduceDeletableItem
    {
        protected  async Task<(ProducerContext Context, ExchangeInfo Exchange)> Init(string fileName)
        {
            var archiveReaderProvider = new FromFileArchiveFileProvider(fileName);
            var archiveReader = (await archiveReaderProvider.Get())!;

            var producerFactory = new ProducerFactory(archiveReaderProvider, FormatSettings.Default);
            var firstExchange = archiveReader.ReadAllExchanges().First();

            var producerContext = (await producerFactory.GetProducerContext(firstExchange.Id))!;

            return (producerContext, firstExchange);
        }
    }
}
