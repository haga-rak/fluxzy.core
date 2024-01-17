// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Fluxzy.Readers;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Packagers
{
    public class FxzyImportEngineTests : ProduceDeletableItem
    {
        [Fact]
        public void Test_Valid()
        {
            var archiveFile = "_Files/Archives/pink-floyd.fxzy"; 
            var packager = new FxzyDirectoryPackager();
            var importEngine = new FxzyImportEngine(packager); 

            var result = importEngine.IsFormat(archiveFile);

            Assert.True(result);
        }
        [Fact]
        public void Test_Invalid_File_Missing()
        {
            var archiveFile = "_Files/Archives/not-a-file.fxzy"; 
            var packager = new FxzyDirectoryPackager();
            var importEngine = new FxzyImportEngine(packager); 

            var result = importEngine.IsFormat(archiveFile);

            Assert.False(result);
        }

        [Fact]
        public void Test_Unpack()
        {
            var validArchiveFile = "_Files/Archives/pink-floyd.fxzy";
            var packager = new FxzyDirectoryPackager();
            var importEngine = new FxzyImportEngine(packager);
            var directory = GetRegisteredRandomDirectory();
            var directory2 = GetRegisteredRandomDirectory();

            importEngine.WriteToDirectory(validArchiveFile, directory); 


            Assert.True(Directory.Exists(directory));
        }

        [Fact]
        public async Task Test_Unpack_Async()
        {
            var validArchiveFile = "_Files/Archives/pink-floyd.fxzy";
            var packager = new FxzyDirectoryPackager();
            var directory = GetRegisteredRandomDirectory();
            await using var inputStream = File.OpenRead(validArchiveFile);

            await packager.UnpackAsync(inputStream, directory); 

            Assert.True(Directory.Exists(directory));
        }

        [Fact]
        public async Task Compress_Decompress()
        {
            var memoryStream = new MemoryStream();

            await ZipHelper.Compress(new (".artefacts/tests/pink-floyd"),
                memoryStream, (_) => true);

            memoryStream.Seek(0, SeekOrigin.Begin);

            var reader = new FluxzyArchiveReader(memoryStream); 

            var exchanges = reader.ReadAllExchanges().ToList();

            Assert.NotEmpty(exchanges);
        }
    }
}
