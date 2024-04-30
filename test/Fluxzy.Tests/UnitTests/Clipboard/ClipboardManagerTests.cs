using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Fluxzy.Clipboard;
using Fluxzy.Readers;
using Fluxzy.Tests._Fixtures;
using Fluxzy.Writers;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Clipboard
{
    public class ClipboardManagerTests : IClassFixture<ClipboardManagerDataFixture>
    {
        private readonly ClipboardManagerDataFixture _testDataFixture;
        private readonly ClipboardManager _clipboardManager = new();

        public ClipboardManagerTests(ClipboardManagerDataFixture testDataFixture)
        {
            _testDataFixture = testDataFixture;
        }

        [Theory]
        [CombinatorialData]
        public async Task AppendToAnEmptyFile(
            [CombinatorialValues(CopyOptionType.Memory, CopyOptionType.Reference)] CopyOptionType copyOptionType,
            [CombinatorialValues(true, false)] bool compress,
            [CombinatorialValues(null,  8 * 1024L * 1024, 1L)] long ? maxSize
            )
        {
            // Arrange 
            using var originalArchiveReader = _testDataFixture.GetArchiveReader(compress);

            var outputDirectory = $"Drop/{nameof(ClipboardManagerTests)}/{Guid.NewGuid()}";
            var copyEnforcer = new CopyPolicyEnforcer(new CopyPolicy(copyOptionType, maxSize, null));

            var expectedExchangeInfo = originalArchiveReader.ReadExchange(_testDataFixture.CopyExchangeId); 

            var directoryArchiveWriter = new DirectoryArchiveWriter(outputDirectory, null);
            directoryArchiveWriter.Init();

            using var actualArchiveReader = new DirectoryArchiveReader(outputDirectory);

            // Act
            var copyData = await _clipboardManager.Copy(
                new[] { _testDataFixture.CopyExchangeId }, 
                originalArchiveReader, 
                copyEnforcer);

            await _clipboardManager.Paste(copyData, directoryArchiveWriter);

            var allExchanges = actualArchiveReader.ReadAllExchanges().ToList();
            var exchange = allExchanges.FirstOrDefault();

            // Assert

            Assert.Single(allExchanges);
            Assert.NotNull(exchange!);

            var shouldCheckExtraAssets =
                (copyOptionType == CopyOptionType.Memory || 
                originalArchiveReader is not FluxzyArchiveReader) && maxSize > 10;

            MakeExchangeComparisonAssertion(
                expectedExchangeInfo, exchange, 
                originalArchiveReader, actualArchiveReader, 
                shouldCheckExtraAssets);

#if DEBUG
            // Convenience for local debugging, not needed for CI
            await using var zipStream = new FileStream($"Drop/{nameof(ClipboardManagerTests)}/last.fxzy", FileMode.Create);
            await ZipHelper.Compress(new DirectoryInfo(outputDirectory), zipStream, (_) => true);
#endif

        }

        private static void MakeExchangeComparisonAssertion(
            ExchangeInfo? expectedExchangeInfo, ExchangeInfo exchange, 
            IArchiveReader expectedArchiveReader,
            IArchiveReader actualArchiveReader,
            bool checkAssets)
        {
            Assert.NotEqual(expectedExchangeInfo!.Id, exchange.Id);
            Assert.NotEqual(expectedExchangeInfo.ConnectionId, exchange.ConnectionId);
            Assert.Equal(expectedExchangeInfo.FullUrl, exchange.FullUrl);
            Assert.Equal(expectedExchangeInfo.StatusCode, exchange.StatusCode);
            Assert.Equal(expectedExchangeInfo.Method, exchange.Method);
            Assert.Equal(expectedExchangeInfo.Metrics.RequestHeaderSent, exchange.Metrics.RequestHeaderSent);

            if (checkAssets)
            {
                Assert.Equal(
                    expectedArchiveReader.GetRequestBody(expectedExchangeInfo.Id)?.DrainAndSha1(),
                    actualArchiveReader.GetRequestBody(exchange.Id)?.DrainAndSha1());

                Assert.Equal(
                    expectedArchiveReader.GetResponseBody(expectedExchangeInfo.Id)?.DrainAndSha1(),
                    actualArchiveReader.GetResponseBody(exchange.Id)?.DrainAndSha1());

                Assert.Equal(
                    expectedArchiveReader.GetRawCaptureStream(expectedExchangeInfo.ConnectionId)?.DrainAndSha1(),
                    actualArchiveReader.GetRawCaptureStream(exchange.ConnectionId)?.DrainAndSha1());

                Assert.Equal(
                    expectedArchiveReader.GetRawCaptureKeyStream(expectedExchangeInfo.ConnectionId)?.DrainAndSha1(),
                    actualArchiveReader.GetRawCaptureKeyStream(exchange.ConnectionId)?.DrainAndSha1());
            }
        }
    }
}
