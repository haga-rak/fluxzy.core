using System.Linq;
using Fluxzy.Readers;
using Xunit;

namespace Fluxzy.Tests.UnitTests.CookieFlows
{
    public class CookieFlowAnalyzerTests
    {
        [Fact]
        public void Test()
        {
            var analyzer = new CookieFlowAnalyzer();

            var url = "_Files/Archives/sample-bin.fxzy";
            var fxzyArchiveReader = new FluxzyArchiveReader(url);
            var exchanges = fxzyArchiveReader.ReadAllExchanges().ToList();

            var result = analyzer.Execute("abc", "httpbin.org", null, exchanges);

        }
    }
}
