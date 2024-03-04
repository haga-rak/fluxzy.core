using System.Linq;
using Fluxzy.Readers;
using Xunit;

namespace Fluxzy.Tests.UnitTests.CookieFlows
{
    public class CookieFlowAnalyzerTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("/")]
        public void Add_Remove_From_Server(string? path)
        {
            var analyzer = new CookieFlowAnalyzer();

            var url = "_Files/Archives/sample-bin.fxzy";
            var fxzyArchiveReader = new FluxzyArchiveReader(url);
            var exchanges = fxzyArchiveReader.ReadAllExchanges().ToList();

            var result = analyzer.Execute("freeform", "httpbin.org", path, exchanges);

            Assert.Equal(6, result.Events.Count);
            Assert.Equal(CookieUpdateType.AddedFromServer, result.Events[0].UpdateType);
            Assert.Equal(CookieUpdateType.RemovedByServer, result.Events[5].UpdateType);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("/")]
        public void Add_Remove_From_Client(string? path)
        {
            var analyzer = new CookieFlowAnalyzer();

            var url = "_Files/Archives/sample-bin.fxzy";
            var fxzyArchiveReader = new FluxzyArchiveReader(url);
            var exchanges = fxzyArchiveReader.ReadAllExchanges().ToList();

            var result = analyzer.Execute("abc", "httpbin.org", path, exchanges);

            Assert.Equal(5, result.Events.Count);
            Assert.Equal(CookieUpdateType.AddedFromServer, result.Events[0].UpdateType);
            Assert.Equal(CookieUpdateType.RemovedByClient, result.Events[4].UpdateType);
        }
    }
}
