// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Archiving.Har;
using Fluxzy.Archiving.Saz;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Archiving
{
    public class PackagerRegistryTests
    {
        [Theory]
        [InlineData("yoyo.fxzy", nameof(FxzyDirectoryPackager))]
        [InlineData("yoyo.saz", nameof(SazPackager))]
        [InlineData("yoyo.har", nameof(HttpArchivePackager))]
        [InlineData("yoyo.invalid", nameof(FxzyDirectoryPackager))]
        public void Test(string fileName, string typeName)
        {
            var packager = PackagerRegistry.Instance.InferPackagerFromFileName(fileName);

            Assert.Equal(packager.GetType().Name, typeName);
        }
    }
}
