// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Readers;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Packagers
{
    public class ImportEngineProviderTests
    {
        private readonly ImportEngineProvider _importEngineProvider;

        public ImportEngineProviderTests()
        {
            _importEngineProvider = new ImportEngineProvider(new FxzyDirectoryPackager());
        }

        [Theory]
        [InlineData(nameof(FxzyImportEngine), "_Files/Archives/pink-floyd.fxzy")]
        [InlineData(nameof(HarImportEngine), "_Files/Archives/minimal.har")]
        [InlineData(nameof(SazImportEngine), "_Files/Archives/minimal.saz")]
        public void Test(string className, string filename)
        {
            var importEngine = _importEngineProvider.GetImportEngine(filename);

            Assert.NotNull(importEngine);
            Assert.Equal(className, importEngine.GetType().Name);
        }
    }
}
