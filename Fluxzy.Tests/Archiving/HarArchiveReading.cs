// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Fluxzy.Readers;
using Fluxzy.Tests._Files;
using Fluxzy.Tests.Common;
using Xunit;

namespace Fluxzy.Tests.Archiving
{
    public class HarArchiveReading : ProduceDeletableItem
    {
        public HarArchiveReading()
        {
            DisablePurge = false; 
        }

        [Fact]
        public async Task TestMinimalArchive()
        {
            var outputDirectory = RegisterDirectory("minimal-har");
            var inputFile = RegisterFile("minimal.har");

            File.WriteAllBytes(inputFile, StorageContext.minimal1);

            var importEngine = new HarImportEngine();

            importEngine.WriteToDirectory(inputFile, outputDirectory);

            using var directoryArchiveReader = new DirectoryArchiveReader(outputDirectory);

            var exchanges = directoryArchiveReader.ReadAllExchanges().ToList();
            var connections = directoryArchiveReader.ReadAllConnections().ToList();
            

            var packager = new FxzyDirectoryPackager();

            await packager.Pack(outputDirectory, RegisterFile(@"sortie-har.fxzy"));
        }
    }
}
