// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Fluxzy.Readers;
using Fluxzy.Tests._Files;
using Fluxzy.Tests.Common;
using Xunit;

namespace Fluxzy.Tests.Archiving
{
    public class HttpArchiveReading : ProduceDeletableItem
    {
        public HttpArchiveReading()
        {
            DisablePurge = true; 
        }

        [Fact]
        public async Task TestMinimalArchive()
        {
            var outputDirectory = RegisterDirectory("minimal-har" + Guid.NewGuid());
            var inputFile = RegisterFile("minimal.har");

            await File.WriteAllBytesAsync(inputFile, StorageContext.sandbox_smartizy_com);

            var importEngine = new HarImportEngine();

            importEngine.WriteToDirectory(inputFile, outputDirectory);

            using var directoryArchiveReader = new DirectoryArchiveReader(outputDirectory);

            var exchanges = directoryArchiveReader.ReadAllExchanges().ToList();
            var connections = directoryArchiveReader.ReadAllConnections().ToList();

            var packager = new FxzyDirectoryPackager();


            Assert.Equal(3, exchanges.Count);

            await packager.Pack(outputDirectory, RegisterFile(@"d:\sortie-har.fxzy"));
        }
    }
}
