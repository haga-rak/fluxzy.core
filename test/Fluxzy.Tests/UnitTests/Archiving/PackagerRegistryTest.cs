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
        public void Infer(string fileName, string typeName)
        {
            var packager = PackagerRegistry.Instance.InferPackagerFromFileName(fileName);

            Assert.Equal(packager.GetType().Name, typeName);
        }

        [Theory]
        [InlineData("fluxzy", nameof(FxzyDirectoryPackager))]
        [InlineData("har", nameof(HttpArchivePackager))]
        [InlineData("saz", nameof(SazPackager))]
        [InlineData("invalid", nameof(FxzyDirectoryPackager))]
        public void GetPackageOrDefault(string requestedName, string typeName)
        {
            var packager = PackagerRegistry.Instance.GetPackageOrDefault(requestedName);

            Assert.Equal(packager.GetType().Name, typeName);
        }
    }
}
