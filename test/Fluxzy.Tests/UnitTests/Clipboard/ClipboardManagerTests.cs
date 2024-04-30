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
    public class ClipboardManagerTests
    {
        private readonly ClipboardManager _clipboardManager = new();

        [Fact]
        public async Task AppendToAnEmptyFile()
        {
            var sessionId = Guid.NewGuid();
            var fileSource = "_Files/Archives/with-request-payload.fxzy"; 
            var outputDirectory = $"Drop/{nameof(ClipboardManagerTests)}/{sessionId}";

            var originalArchiveReader = new FluxzyArchiveReader(fileSource); 
            var copyEnforcer = new CopyPolicyEnforcer(new CopyPolicy(CopyOptionType.Memory, null, null));

            var exchangeId = 101;

            var expectedExchangeInfo = originalArchiveReader.ReadExchange(exchangeId); 

            var directoryArchiveWriter = new DirectoryArchiveWriter(outputDirectory, null);
            directoryArchiveWriter.Init();

            var copyData = await _clipboardManager.Copy(new[] { exchangeId }, originalArchiveReader, copyEnforcer);

            await _clipboardManager.Paste(copyData, directoryArchiveWriter);

            var finalDirectoryArchiveReader = new DirectoryArchiveReader(outputDirectory);

            var allExchanges = finalDirectoryArchiveReader.ReadAllExchanges().ToList();
            var exchange = allExchanges.FirstOrDefault();

            Assert.Single(allExchanges);
            Assert.NotNull(exchange!);

            MakeExchangeComparisonAssertion(expectedExchangeInfo, exchange, originalArchiveReader, finalDirectoryArchiveReader);

#if DEBUG
            // Convenience for local debugging, not needed for CI
            await using var zipStream = new FileStream($"Drop/{nameof(ClipboardManagerTests)}/last.fxzy", FileMode.Create);
            await ZipHelper.Compress(new DirectoryInfo(outputDirectory), zipStream, (_) => true);
#endif

        }

        private static void MakeExchangeComparisonAssertion(
            ExchangeInfo? expectedExchangeInfo, ExchangeInfo exchange, 
            IArchiveReader expectedArchiveReader,
            IArchiveReader actualArchiveReader)
        {
            Assert.NotEqual(expectedExchangeInfo!.Id, exchange.Id);
            Assert.NotEqual(expectedExchangeInfo.ConnectionId, exchange.ConnectionId);
            Assert.Equal(expectedExchangeInfo.FullUrl, exchange.FullUrl);
            Assert.Equal(expectedExchangeInfo.StatusCode, exchange.StatusCode);
            Assert.Equal(expectedExchangeInfo.Method, exchange.Method);
            Assert.Equal(expectedExchangeInfo.Metrics.RequestHeaderSent, exchange.Metrics.RequestHeaderSent);

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
