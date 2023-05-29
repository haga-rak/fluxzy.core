// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Fluxzy.Misc.Streams;
using Fluxzy.Readers;
using Fluxzy.Tests._Files;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.Archiving
{
    public class HttpArchiveReading : ProduceDeletableItem
    {
        public HttpArchiveReading()
        {
            DisablePurge = false;
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

            await packager.Pack(outputDirectory, RegisterFile(@"sortie-har.fxzy"));
        }

        [Fact]
        public async Task TestWithRequestPayload()
        {
            var outputDirectory = RegisterDirectory("request-payload" + Guid.NewGuid());
            var inputFile = RegisterFile("request-payload.har");

            await File.WriteAllBytesAsync(inputFile, StorageContext.with_payload);

            var importEngine = new HarImportEngine();

            importEngine.WriteToDirectory(inputFile, outputDirectory);

            using var directoryArchiveReader = new DirectoryArchiveReader(outputDirectory);

            var exchanges = directoryArchiveReader.ReadAllExchanges().ToList();
            var connections = directoryArchiveReader.ReadAllConnections().ToList();

            var packager = new FxzyDirectoryPackager();

            var requestBody = directoryArchiveReader.GetRequestBody(exchanges[0].Id)?.ReadToEndGreedy();

            Assert.Single(exchanges);
            Assert.Equal("POST", exchanges[0].Method, StringComparer.OrdinalIgnoreCase);
            Assert.Equal("https://httpbin.org/post", exchanges[0].FullUrl, StringComparer.OrdinalIgnoreCase);
            Assert.NotNull(requestBody);

            Assert.Contains("application/x-www-form-urlencoded",
                exchanges[0].GetRequestHeaders()
                            .Where(s => s.Name.Span.Equals("content-type", StringComparison.OrdinalIgnoreCase))
                            .Select(s => s.Value.ToString()));

            Assert.Equal(
                "custname=ab&custtel=cd&custemail=abc%40abc.com&size=medium&topping=cheese&topping=onion&delivery=11%3A45&comments=bloblo",
                requestBody);

            await packager.Pack(outputDirectory, RegisterFile($@"{nameof(TestWithRequestPayload)}.fxzy"));
        }
    }
}
