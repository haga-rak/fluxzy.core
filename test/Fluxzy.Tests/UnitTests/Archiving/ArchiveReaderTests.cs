// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Linq;
using Fluxzy.Misc.Streams;
using Fluxzy.Readers;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Archiving
{
    public abstract class ArchiveReaderTests
    {
        private readonly IArchiveReader _archiveReader;

        protected ArchiveReaderTests(IArchiveReader archiveReader)
        {
            _archiveReader = archiveReader;
        }

        [Fact]
        public void ReadMetaInformation()
        {
            var metaInformation = _archiveReader.ReadMetaInformation();
            Assert.NotNull(metaInformation);
            Assert.NotNull(metaInformation.ArchiveVersion);
            Assert.NotNull(metaInformation.FluxzyVersion);
            Assert.NotNull(metaInformation.Tags);
            Assert.NotNull(metaInformation.ViewFilters);
            Assert.NotEqual(default, metaInformation.CaptureDate);
        }

        [Fact]
        public void Invalid_Exchange()
        {
            var result = _archiveReader.ReadExchange(9556461);
            Assert.Null(result);
        }

        [Fact]
        public void Read_DownStreamErrors()
        {
            var exchanges = _archiveReader.ReaderAllDownstreamErrors()?.ToList();

            Assert.NotNull(exchanges!);
        }

        [Fact]
        public void Read_Exchanges()
        {
            var exchanges = _archiveReader.ReadAllExchanges()?.ToList();

            Assert.NotNull(exchanges!);
            Assert.NotEmpty(exchanges);

            foreach (var exchange in exchanges) {
                var result = _archiveReader.ReadExchange(exchange.Id);
                Assert.NotNull(result);
            }
        }

        [Fact]
        public void Validate_Connections()
        {
            var connections = _archiveReader.ReadAllConnections()?.ToList();

            Assert.NotNull(connections!);
            Assert.NotEmpty(connections);

            foreach (var connection in connections) {
                var result = _archiveReader.ReadConnection(connection.Id);
                Assert.NotNull(result);

                _ = _archiveReader.HasResponseBody(connection.Id);
                _ = _archiveReader.HasRequestBody(connection.Id);

                Assert.True(_archiveReader.HasCapture(connection.Id));

                using var rawCaptureStream = _archiveReader.GetRawCaptureStream(connection.Id);
                using var rawCaptureKeyStream = _archiveReader.GetRawCaptureKeyStream(connection.Id);

                Assert.NotNull(rawCaptureStream!);
                Assert.NotNull(rawCaptureKeyStream!);
                var length = rawCaptureStream.Drain();

                var keyStreamLength = rawCaptureKeyStream.Drain();

                Assert.True(length > 0);
                Assert.True(keyStreamLength > 0);

                if (_archiveReader is DirectoryArchiveReader directoryArchiveReader) {
                    var rawCaptureFile = directoryArchiveReader.GetRawCaptureFile(connection.Id);
                    Assert.NotNull(rawCaptureFile!);
                }
            }
        }
    }

    public class FluxzyArchiveReaderTests : ArchiveReaderTests
    {
        public FluxzyArchiveReaderTests()
            : base(new FluxzyArchiveReader("_Files/Archives/with-request-payload.fxzy"))
        {
        }
    }
}
