// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Threading.Tasks;
using Fluxzy.Readers;
using Fluxzy.Rules;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters;
using Fluxzy.Rules.Filters.ViewOnlyFilters;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Rules
{
    public class SearchTextFilterTests : FilterTestTemplate
    {
        [Theory]
        [InlineData(true,
            "20px;width:1.4285714em;height:1", false, false, true, false)]
        [InlineData(false,
            "20px;width:1.4898sd46285714em;height:1", false, false, true, false)]
        [InlineData(true,
            "he;desc=\"hit-front\", host;desc=", false, false, false, true)]
        [InlineData(false,
            "he;desc=\"hit-front\", host;desc=", false, false, true, false)]
        [InlineData(true,
            "oIP=FR:IDF:Maisons-Laffitte:48.95:2.15:v4", false, true, false, false)]
        [InlineData(false,
            "oIP=FR:IDF:Maisons-Laffitte:48.95:2.15:v4", false, false, true, false)]
        public async Task CheckPass_SearchTextFilter(
            bool expectedResult, string pattern,
            bool searchInRequestBody, bool searchInRequestHeader,
            bool searchInResponseBody, bool searchInResponseHeader)
        {
            var archiveFile = "_Files/Archives/pink-floyd.fxzy";
            var exchangeId = 45;

            var filter = new SearchTextFilter(pattern) {
                SearchInRequestBody = searchInRequestBody,
                SearchInRequestHeader = searchInRequestHeader,
                SearchInResponseBody = searchInResponseBody,
                SearchInResponseHeader = searchInResponseHeader
            };

            var archiveReader = new FluxzyArchiveReader(archiveFile);
            var exchange = archiveReader.ReadExchange(exchangeId)!;
            var filteringContext = new ExchangeInfoFilteringContext(archiveReader, exchangeId);

            var rule = new Rule(new ApplyCommentAction("hello"), filter);

            var actualResult = filter.Apply(null, null!, exchange, filteringContext);

            Assert.Equal(expectedResult, actualResult);
        }

        [Theory]
        [InlineData(true,
            "zozoz45", true, true, true, true)]
        [InlineData(true,
            "zozoz45", true, false, false, false)]
        [InlineData(false,
            "zozozy45", true, false, false, false)]
        [InlineData(false,
            "zozoz45", false, true, true, true)]
        public void CheckPass_SearchTextFilter_Request_Body(
            bool expectedResult, string pattern,
            bool searchInRequestBody, bool searchInRequestHeader,
            bool searchInResponseBody, bool searchInResponseHeader)
        {
            var archiveFile = "_Files/Archives/with-request-payload.fxzy";
            var exchangeId = 103;

            var filter = new SearchTextFilter(pattern) {
                SearchInRequestBody = searchInRequestBody,
                SearchInRequestHeader = searchInRequestHeader,
                SearchInResponseBody = searchInResponseBody,
                SearchInResponseHeader = searchInResponseHeader,
                CaseSensitive = searchInRequestHeader
            };

            var archiveReader = new FluxzyArchiveReader(archiveFile);
            var exchange = archiveReader.ReadExchange(exchangeId)!;
            var filteringContext = new ExchangeInfoFilteringContext(archiveReader, exchangeId);

            var rule = new Rule(new ApplyCommentAction("hello"), filter);

            var actualResult = filter.Apply(null, null!, exchange, filteringContext);

            Assert.Equal(expectedResult, actualResult);
        }
    }
}
