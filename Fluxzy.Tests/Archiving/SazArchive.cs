// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Fluxzy.Misc.Streams;
using Fluxzy.Readers;
using Fluxzy.Tests._Files;
using Fluxzy.Tests.Common;
using Xunit;

namespace Fluxzy.Tests.Archiving
{
    public class SazArchive : ProduceDeletableItem
    {
        public SazArchive()
        {
            DisablePurge = true; 
        }

        [Fact]
        public void TestValidArchiveFile()
        {
            var inputFile = RegisterFile("archive-saz-test.saz");

            File.WriteAllBytes(inputFile, StorageContext.testarchive);
            
            var sazArchiveReader = new SazArchiveReader();

            Assert.True(sazArchiveReader.IsSazArchive(inputFile));
        }

        [Fact]
        public async Task TestMinimalArchive()
        {
            var outputDirectory = RegisterDirectory("minimal-archive-saz-test");
            var inputFile = RegisterFile("minimal-archive-saz-test.saz");

            File.WriteAllBytes(inputFile, StorageContext.minimal);

            var sazArchiveReader = new SazArchiveReader();
            sazArchiveReader.WriteToDirectory(inputFile, outputDirectory);

            using var directoryArchiveReader = new DirectoryArchiveReader(outputDirectory);

            var exchanges = directoryArchiveReader.ReadAllExchanges().ToList();
            var connections = directoryArchiveReader.ReadAllConnections().ToList();

            Assert.Equal(3, exchanges.Count);
            Assert.Equal(3, connections.Count);

            Assert.Equal("http://sandbox.smartizy.com:8899/global-health-check", exchanges[0].FullUrl);
            Assert.Equal("https://sandbox.smartizy.com/ip", exchanges[1].FullUrl);
            Assert.Equal("https://sandbox.smartizy.com:5001/content-produce/1035/1035", exchanges[2].FullUrl);

            Assert.Equal("POST", exchanges[0].Method);
            Assert.Equal("GET", exchanges[1].Method);
            Assert.Equal("GET", exchanges[2].Method);
            
            Assert.Contains(exchanges[0].GetRequestHeaders(),
                h => h.Name.ToString().Equals("User-Agent") 
                     && h.Value.ToString().Equals("PostmanRuntime/7.31.3"));
            
            Assert.Contains(exchanges[2].GetRequestHeaders(),
                h => h.Name.ToString().Equals("coco") 
                     && h.Value.ToString().Equals("belou"));

            Assert.Contains(exchanges[2].GetResponseHeaders()!,
                h => h.Name.ToString().Equals("Server") 
                     && h.Value.ToString().Equals("Kestrel"));

            var exchange1RequestBody = directoryArchiveReader.GetRequestBody(exchanges[0].Id)
                                                             ?.ReadToEndGreedy(); 

            var exchange2RequestBody = directoryArchiveReader.GetRequestBody(exchanges[1].Id)
                                                             ?.ReadToEndGreedy(); 

            var exchange3RequestBody = directoryArchiveReader.GetRequestBody(exchanges[2].Id)
                                                             ?.ReadToEndGreedy(); 

            var exchange2ResponseBody = directoryArchiveReader.GetResponseBody(exchanges[1].Id)
                                                             ?.ReadToEndGreedy(); 

            Assert.Equal(string.Empty, exchange1RequestBody);
            Assert.Equal("dsds=dsd&dq=sss&zeezez=sdfsdf", exchange2RequestBody);
            Assert.Equal(string.Empty, exchange3RequestBody);
            Assert.Equal("::ffff:127.0.0.1", exchange2ResponseBody);


            var packager = new FxzyDirectoryPackager();

            await packager.Pack(outputDirectory, @"d:\sortie.fxzy");
        }
    }
}
